#!/bin/bash
set -e

# git-pull the two source packages to ensure latest code snapshot
git -C ~/gitlab/docs-site pull --ff-only
git -C ~/gitlab/dpe-date-domain pull --ff-only

# Build the Java extractor code and use it to parse three dirs: model, model/locationmaster and entity.
mvn -q -s central-settings.xml clean package

java -jar target/javaparser-minimal-1.0.0.jar ~/gitlab/dpe-date-domain/src/main/java/com/gehealthcare/dpsa/model /tmp/datamodel/java_models.yaml
java -jar target/javaparser-minimal-1.0.0.jar ~/gitlab/dpe-date-domain/src/main/java/com/gehealthcare/dpsa/model/locationmaster /tmp/datamodel/java_model_location.yaml
java -jar target/javaparser-minimal-1.0.0.jar ~/gitlab/dpe-date-domain/src/main/java/com/gehealthcare/dpsa/entity /tmp/datamodel/java_entities.yaml

# Run the C# Data Model Visualizer
dotnet run --project ../datamodel -- ge-java

# Zip-up the generated output
pushd /tmp
zip -r datamodel.zip datamodel
popd

# Copy the zip file to a location available to all
