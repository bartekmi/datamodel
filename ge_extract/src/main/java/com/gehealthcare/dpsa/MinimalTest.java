package com.gehealthcare.dpsa;

import com.github.javaparser.JavaParser;
import com.github.javaparser.ast.CompilationUnit;
import com.github.javaparser.ast.body.FieldDeclaration;

public class MinimalTest {
    public static void main(String[] args) {
        String source = """
        public class MyClass {
          /**
           * Some random text
           */
          @PartitionKey(prefix = EntityPrefix.APPOINTMENT)
          private String appointmentId;
        }
        """;

        JavaParser parser = new JavaParser();
        CompilationUnit cu = parser.parse(source).getResult().get();

        FieldDeclaration field = cu.findFirst(FieldDeclaration.class,
                        f -> f.getVariables().get(0).getNameAsString().equals("appointmentId"))
                .get();

        // Extract JavaDoc
        field.getJavadoc().ifPresent(javadoc ->
                System.out.println("JavaDoc: " + javadoc.getDescription().toText())
        );

        // Extract annotations
        field.getAnnotations().forEach(annotation ->
                System.out.println("Annotation: " + annotation)
        );
    }
}
