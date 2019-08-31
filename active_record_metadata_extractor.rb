# Main entry point class into the metadata extractor
class ActiveRecordMetadataExtractor
  IGNORE = %w[ApplicationRecord ActiveStorage::Attachment ActiveStorage::Blob].freeze
  OUTPUT_FILE = "bartek_raw_2.txt".freeze

  def self.run
    load_application(".")

    dirty_models = ActiveRecord::Base.descendants
      .reject { |model| IGNORE.any? { |ignorable| model.name.include?(ignorable) } }
    puts "Loaded #{dirty_models.length} models"
    pure_models = ErdValidator.new(dirty_models)

    File.open(OUTPUT_FILE, "w") do |file|
      # write_models file, pure_models
      write_associations file, pure_models
    end
  end

  def self.write_models(file, pure_models)
    file.puts "entities:"
    pure_models.models.each do |model|
      puts model.name
      file.puts "  - entity_name: #{model.name}"
      file.puts "    is_abstract: #{model.abstract_class?}"
      file.puts "    table_name: #{model.table_name}"
      file.puts "    superclass: #{model.superclass.name}"

      write_columns file, model
    end
  end

  def self.write_columns(file, model)
    file.puts "    columns:"
    model.columns.each do |column|
      name = column.name

      file.puts "      - name: #{name}"
      file.puts "        type: #{column.type || column.sql_type.downcase.to_sym}"
      file.puts "        null: #{column.null}"
      file.puts "        validations: #{model.validators_on(name).map(&:kind).join(', ')}"
    end
  rescue StandardError => e
    warn("Could not write columns for model #{model.name} (#{e.message})")
  end

  def self.write_associations(file, pure_models)
    file.puts "associations:"
    pure_models.associations.each do |association|
      file.puts "  - kind: #{association.class.name.split('::').last}"
      file.puts "    name: #{association.name}"
      file.puts "    active_record: #{association.active_record}"
      file.puts "    class_name: #{association.class_name}" # Not fully-qualified
      file.puts "    foreign_key: #{association.foreign_key}"
      file.puts "    inverse_of: #{association.inverse_of ? association.inverse_of.name : nil}"
      file.puts "    plural_name: #{association.plural_name}"
      file.puts "    options: '#{association.options}'"
      file.puts "    klass: #{association.klass}" # This one is fully-qualified

      # Currently unused
      file.puts "    foreign_type: #{association.foreign_type}"
      file.puts "    type: '#{association.type}'"
    rescue StandardError => e
      warn("Could not write association #{ErdValidator.association_description(association)} (#{e.message})")
    end
  end

  # This code borrowed from rails-erd gem file: cli.rb
  def self.load_application(path)
    warn("Loading application...")
    environment_path = "#{path}/config/environment.rb"

    begin
      require environment_path
    rescue ::LoadError
      puts "Please create a file in '#{environment_path}' that loads your application environment."
      raise
    end

    Rails.application.eager_load!
    Rails.application.config.eager_load_namespaces.each(&:eager_load!) if Rails.application.config.respond_to?(:eager_load_namespaces)
  end
end

# This code borrowed from rails-erd gem file: cli.rb
class ErdValidator
  def initialize(models)
    @source_models = models
  end

  def models
    @models ||= @source_models.select { |model| check_model_validity(model) }.reject { |model| check_habtm_model(model) }
  end

  # Returns a specific entity object for the given Active Record model.
  def entity_by_name(name) # @private :nodoc:
    entity_mapping[name]
  end

  def entity_mapping
    @entity_mapping ||= {}.tap do |mapping|
      models.each do |entity|
        mapping[entity.name] = entity
      end
    end
  end

  def check_model_validity(model)
    if model.abstract_class? || model.table_exists?
      if model.name.nil?
        raise "is anonymous class"
      else
        true
      end
    else
      raise "table #{model.table_name} does not exist"
    end
  rescue StandardError => e
    warn("Ignoring invalid model #{model.name} (#{e.message})")
  end

  def check_habtm_model(model)
    model.name.start_with?("HABTM_")
  end

  def associations
    # @associations ||= models.collect(&:reflect_on_all_associations).flatten.select { |assoc| check_association_validity(assoc) }
    @associations ||= models.collect(&:reflect_on_all_associations).flatten
  end

  def check_association_validity(association)
    # Raises an ActiveRecord::ActiveRecordError if the association is broken.
    association.check_validity!

    if association.options[:polymorphic]
      entity_name = association.class_name
      entity_by_name(entity_name) || raise("polymorphic interface #{entity_name} does not exist")
    else
      entity_name = association.klass.name # Raises NameError if the associated class cannot be found.
      entity_by_name(entity_name) || raise("model #{entity_name} exists, but is not included in domain")
    end
  rescue StandardError => e
    warn("Ignoring invalid association #{association_description(association)} (#{e.message})")
  end

  def self.association_description(association)
    "#{association.name.inspect} on #{association.active_record}"
  end
end

ActiveRecordMetadataExtractor.run
