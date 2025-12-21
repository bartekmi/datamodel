# Overview

Extract information from Json model files in dpsa and store them in YAML format

# Build / Run Instructions
## IntelliJ Setup
If you plan to debug, tell IntelliJ to use the local Maven settings file:
1. Go to Settings → Build, Execution, Deployment → Build Tools → Maven.
2. Under User settings file, point it to your central-settings.xml instead of the default ~/.m2/settings.xml.
3. Click Apply, then Reload All Maven Projects in the Maven tool window.

## To Build...
* mvn -q -s central-settings.xml clean package

## To Run...
* java -jar target/javaparser-minimal-1.0.0.jar ~/gitlab/dpe-date-domain/src/main/java/com/gehealthcare/dpsa/model /tmp/datamodel/java_models.yaml
* java -jar target/javaparser-minimal-1.0.0.jar ~/gitlab/dpe-date-domain/src/main/java/com/gehealthcare/dpsa/entity /tmp/datamodel/java_entities.yaml