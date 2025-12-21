
package com.gehealthcare.dpsa;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.dataformat.yaml.YAMLFactory;
import com.fasterxml.jackson.dataformat.yaml.YAMLGenerator;
import com.github.javaparser.JavaParser;
import com.github.javaparser.ast.CompilationUnit;
import com.github.javaparser.ast.body.ClassOrInterfaceDeclaration;
import com.github.javaparser.ast.body.FieldDeclaration;
import com.github.javaparser.ast.body.MethodDeclaration;
import com.github.javaparser.ast.body.VariableDeclarator;
import com.github.javaparser.ast.nodeTypes.NodeWithAnnotations;
import com.github.javaparser.ast.nodeTypes.NodeWithJavadoc;
import com.github.javaparser.javadoc.Javadoc;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class JavaClassIntrospector {

    /**
     * Usage:
     *   java -jar .../javaparser-minimal.jar /path/to/dir [output.yaml]
     *
     * Behavior:
     *   - Iterates ONLY the immediate files in the provided directory (no recursion).
     *   - Parses each .java file that contains exactly one top-level class (non-interface).
     *   - Extracts private fields and public static methods (with Javadoc).
     *   - Writes a YAML file. If output path is omitted, writes "extraction.yaml" in CWD.
     */
    public static void main(String[] args) throws Exception {
        if (args.length != 2) {
            System.err.println("Usage: JavaClassIntrospector <source-dir> <output-yaml>");
            System.exit(2);
        }

        Path dir = Path.of(args[0]).toAbsolutePath().normalize();
        if (!Files.isDirectory(dir)) {
            throw new IllegalArgumentException("Not a directory: " + dir);
        }

        Path output = Path.of(args[1]).toAbsolutePath().normalize();

        Extraction extraction = parseDirectory(dir);

        // Serialize to YAML
        ObjectMapper yaml = new ObjectMapper(new YAMLFactory()
                .disable(YAMLGenerator.Feature.WRITE_DOC_START_MARKER)
                .enable(YAMLGenerator.Feature.LITERAL_BLOCK_STYLE)
        );
        yaml.setSerializationInclusion(JsonInclude.Include.NON_EMPTY);
        String yamlText = yaml.writerWithDefaultPrettyPrinter().writeValueAsString(extraction);

        // Write file and echo summary
        Files.writeString(output, yamlText);
        System.out.println("Processed " + extraction.classInfos.size() + " Java file(s) in: " + dir);
        System.out.println("YAML written to: " + output);
    }

    /**
     * Parse all immediate .java files in the given directory (no recursion).
     */
    public static Extraction parseDirectory(Path dir) {
        List<ClassInfo> classInfos = new ArrayList<>();

        try (Stream<Path> files = Files.list(dir)) {
            // Only regular files ending in .java
            List<Path> javaFiles = files
                    .filter(Files::isRegularFile)
                    .filter(p -> p.getFileName().toString().endsWith(".java"))
                    .toList();

            for (Path file : javaFiles) {
                try {
                    ClassInfo r = parseSingleClassFile(file);
                    classInfos.add(r);
                } catch (Exception ex) {
                    // Skip problematic files but log a concise message
                    System.err.println("[WARN] Skipping " + file.getFileName() + ": " + ex.getMessage());
                }
            }
        } catch (IOException e) {
            throw new RuntimeException("Failed to list directory: " + dir, e);
        }

        Extraction extraction = new Extraction();
        extraction.directory = dir.toString();
        extraction.classInfos = classInfos;
        return extraction;
    }

    /**
     * Parse a single Java source file that contains exactly one top-level class (non-interface).
     */
    public static ClassInfo parseSingleClassFile(Path file) throws IOException {
        String source = Files.readString(file);

//        ParserConfiguration config = new ParserConfiguration()
//                .setAttributeComments(false)
//                .setDoNotAssignCommentsPrecedingEmptyLines(false);

        JavaParser parser = new JavaParser();
        CompilationUnit cu = parser.parse(source).getResult()
                .orElseThrow(() -> new IllegalArgumentException("Unable to parse " + file));

        List<ClassOrInterfaceDeclaration> classes = cu.findAll(ClassOrInterfaceDeclaration.class)
                .stream()
                .filter(c -> !c.isInterface())
                .toList();

        if (classes.size() != 1) {
            throw new IllegalStateException("Expected exactly one top-level class, found: " + classes.size());
        }

        ClassOrInterfaceDeclaration clazz = classes.getFirst();
        String javaDoc = clazz.getJavadoc().map(Javadoc::toText).orElse(null);
        ClassInfo r = new ClassInfo(file.toString(), clazz.getNameAsString(), javaDoc);

        // Private fields (properties)
        for (FieldDeclaration fd : clazz.getFields()) {
            if (fd.isPrivate()) {

//                List<AnnotationInfo> declAnns = fd.getAnnotations().stream()
//                        .map(JavaClassIntrospector::toAnnotationInfo)
//                        .toList();
//                System.out.println(declAnns);

                String type = fd.getElementType().asString();
                for (VariableDeclarator var : fd.getVariables()) {
                    FieldInfo fi = new FieldInfo(var.getNameAsString(), type, null);
                    enrichWithDocsAndAnnotations(fi, fd);
                    r.privateFields.add(fi);
                }
            }
        }

        // Public static methods
        for (MethodDeclaration m : clazz.getMethods()) {
            if (m.isPublic() && m.isStatic()) {
                String javadoc = m.getJavadoc().map(Javadoc::toText).orElse(null);
                List<String> paramTypes = m.getParameters().stream()
                        .map(p -> p.getType().asString())
                        .collect(Collectors.toList());
                r.publicStaticMethods.add(
                        new MethodInfo(m.getNameAsString(), m.getType().asString(), paramTypes, javadoc)
                );
            }
        }

        return r;
    }

    // --- data holders for YAML output ---

    /** Top-level container holding all file Results. */
    public static class Extraction {
        public String directory;
        public List<ClassInfo> classInfos = new ArrayList<>();
    }

    public static class JavaEntityBase {
        public String javaDoc;
        public List<String> annotations = new ArrayList<>();
        public JavaEntityBase(String javaDoc) {
            this.javaDoc = javaDoc;
        }
    }

    /** Per-class extraction result. */
    public static class ClassInfo extends JavaEntityBase {
        public String sourceFile; // helpful to know which file this came from
        public String className; // helpful to know which file this came from
        public List<FieldInfo> privateFields = new ArrayList<>();
        public List<MethodInfo> publicStaticMethods = new ArrayList<>();

        public ClassInfo(String sourceFile, String className, String javadoc) {
            super(javadoc);
            this.sourceFile = sourceFile;
            this.className = className;
        }
    }

    public static class FieldInfo extends JavaEntityBase {
        public final String name;
        public final String type;
        public FieldInfo(String name, String type, String javadoc) {
            super(javadoc);
            this.name = name;
            this.type = type;;
        }
    }

    public static class MethodInfo extends JavaEntityBase {
        public final String name;
        public final String returnType;
        public final List<String> parameterTypes;
        public MethodInfo(String name, String returnType, List<String> parameterTypes, String javadoc) {
            super(javadoc);
            this.name = name;
            this.returnType = returnType;
            this.parameterTypes = parameterTypes;
        }
    }

    // Utilities
    private static <T extends NodeWithJavadoc<?>> void enrichWithDocsAndAnnotations(JavaEntityBase target, T node) {
        // Javadoc
        target.javaDoc = getJavadoc(node);

        // Annotations (class, field, method all implement NodeWithAnnotations)
        if (node instanceof NodeWithAnnotations<?> nwa) {
            for (var ann : nwa.getAnnotations()) {
                target.annotations.add(ann.toString());
            }
        }
    }

    private static <T extends NodeWithJavadoc<?>> String getJavadoc(T node) {
            return node.getJavadoc()
                    .map(j -> j.getDescription().toText())
                    .orElse(null);
    }
}
