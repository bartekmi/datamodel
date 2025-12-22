#!/bin/bash
mvn -q -s central-settings.xml clean package

java -jar target/javaparser-minimal-1.0.0.jar ~/gitlab/dpe-date-domain/src/main/java/com/gehealthcare/dpsa/model /tmp/datamodel/java_models.yaml
java -jar target/javaparser-minimal-1.0.0.jar ~/gitlab/dpe-date-domain/src/main/java/com/gehealthcare/dpsa/entity /tmp/datamodel/java_entities.yaml