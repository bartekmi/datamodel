# Main entry point class into the metadata extractor
class ActiveRecordMetadataExtractor
  IGNORE = %w[ApplicationRecord ActiveStorage::Attachment ActiveStorage::Blob].freeze
  OUTPUT_FILE = "bartek_raw_2.txt".freeze

  def self.run
    load_application(".")

    models = ActiveRecord::Base.descendants
      .reject { |model| IGNORE.any? { |ignorable| model.name.include?(ignorable) } }
    puts "Loaded #{models.length} models"

    File.open(OUTPUT_FILE, "w") do |file|
      write_models file, models
      write_associations file, models
    end
  end

  def self.write_models(file, models)
    file.puts "entities:"
    models.each do |model|
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

  def self.write_associations(file, models)
    file.puts "associations:"
    models.each do |model|
      model.reflect_on_all_associations.each do |association|
        begin
          file.puts "  - kind: #{association.class.name.split('::').last}"
          file.puts "    name: #{association.name}"
          file.puts "    active_record: #{association.active_record}"
          file.puts "    class_name: #{association.class_name}" # Not fully-qualified
          file.puts "    foreign_key: #{association.foreign_key}"
          file.puts "    inverse_of: #{association.inverse_of ? association.inverse_of.name : nil}"
          file.puts "    plural_name: #{association.plural_name}"
          file.puts "    options: '#{association.options}'"

          # This one is fully-qualified
          file.puts "    klass: #{association.klass}" unless association.polymorphic?

          # Currently unused
          file.puts "    foreign_type: #{association.foreign_type}"
          file.puts "    type: '#{association.type}'"
        rescue StandardError => e
          byebug
          warn("Could not write association #{association} (#{e.message})")
        end
      end
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

ActiveRecordMetadataExtractor.run
