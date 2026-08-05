using Senice.Core.Models;

namespace Senice.Core.Languages;

public sealed class LanguageCatalog
{
    private readonly Dictionary<string, LanguageProfile> _byExtension;
    private readonly Dictionary<string, LanguageProfile> _byFileName;

    public LanguageCatalog()
    {
        var profiles = BuildProfiles();
        _byExtension = new Dictionary<string, LanguageProfile>(StringComparer.OrdinalIgnoreCase);
        _byFileName = new Dictionary<string, LanguageProfile>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in profiles)
        {
            foreach (var extension in profile.Extensions)
                _byExtension.TryAdd(extension, profile);
            foreach (var fileName in profile.FileNames)
                _byFileName.TryAdd(fileName, profile);
        }
    }

    public LanguageProfile? Resolve(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (_byFileName.TryGetValue(fileName, out var byName))
            return byName;

        var extension = Path.GetExtension(filePath);
        if (extension.Length > 1 && _byExtension.TryGetValue(extension[1..], out var byExtension))
            return byExtension;

        return null;
    }

    public IReadOnlyCollection<string> KnownExtensions => _byExtension.Keys;

    public IReadOnlyCollection<string> KnownFileNames => _byFileName.Keys;

    private static readonly StringLiteral[] SlashStrings =
    [
        new("\"", "\"", '\\'),
        new("'", "'", '\\'),
    ];

    private static readonly StringLiteral[] BackslashStrings =
    [
        new("\"", "\"", '\\'),
    ];

    private static readonly StringLiteral[] SqlStrings =
    [
        new("'", "'", DoubledEscape: true),
        new("\"", "\"", DoubledEscape: true),
    ];

    private static readonly StringLiteral[] HtmlStrings =
    [
        new("\"", "\""),
        new("'", "'"),
    ];

    private static readonly StringLiteral[] VbStrings =
    [
        new("\"", "\"", DoubledEscape: true),
    ];

    private static readonly StringLiteral[] PascalStrings =
    [
        new("'", "'", DoubledEscape: true),
        new("\"", "\"", '\\'),
    ];

    private static readonly StringLiteral[] PythonStrings =
    [
        new("\"\"\"", "\"\"\"", '\\', MultiLine: true),
        new("'''", "'''", '\\', MultiLine: true),
        new("\"", "\"", '\\'),
        new("'", "'", '\\'),
    ];

    private static readonly StringLiteral[] ShellStrings =
    [
        new("\"", "\"", '\\'),
        new("'", "'"),
    ];

    private static readonly StringLiteral[] LispStrings =
    [
        new("\"", "\"", '\\'),
    ];

    private static readonly StringLiteral[] FortranStrings =
    [
        new("\"", "\"", DoubledEscape: true),
        new("'", "'", DoubledEscape: true),
    ];

    private static LanguageProfile P(
        string id,
        string name,
        string[]? extensions = null,
        string[]? fileNames = null,
        LineCommentMarker[]? line = null,
        (string Open, string Close)[]? block = null,
        StringLiteral[]? strings = null,
        string? ts = null,
        string? shebang = null,
        bool nested = false,
        bool heredocs = false)
        => new()
        {
            Id = id,
            Name = name,
            Extensions = extensions ?? [],
            FileNames = fileNames ?? [],
            LineComments = line ?? [],
            BlockComments = block?.Select(b => new BlockComment(b.Open, b.Close)).ToArray() ?? [],
            StringLiterals = strings ?? SlashStrings,
            TreeSitterGrammar = ts,
            ShebangMarker = shebang,
            NestedBlockComments = nested,
            SupportsHeredocs = heredocs,
        };

    private static LineCommentMarker[] L(params string[] markers) =>
        markers.Select(m => new LineCommentMarker(m)).ToArray();

    private static LineCommentMarker[] LS(params string[] markers) =>
        markers.Select(m => new LineCommentMarker(m, RequiresLineStart: true)).ToArray();

    private static List<LanguageProfile> BuildProfiles() => new()
    {
        P("c", "C", ["c", "h"], line: L("//"), block: [("/*", "*/")], ts: "c"),
        P("cpp", "C++", ["cpp", "cc", "cxx", "c++", "hpp", "hh", "hxx", "ipp", "tpp", "inl"], line: L("//"), block: [("/*", "*/")], ts: "cpp"),
        P("csharp", "C#", ["cs"], line: L("//"), block: [("/*", "*/")], ts: "c-sharp"),
        P("java", "Java", ["java"], line: L("//"), block: [("/*", "*/")], ts: "java"),
        P("javascript", "JavaScript", ["js", "mjs", "cjs"], line: L("//"), block: [("/*", "*/")], ts: "javascript"),
        P("jsx", "JSX", ["jsx"], line: L("//"), block: [("/*", "*/")], ts: "javascript"),
        P("typescript", "TypeScript", ["ts", "mts", "cts"], line: L("//"), block: [("/*", "*/")], ts: "typescript"),
        P("tsx", "TSX", ["tsx"], line: L("//"), block: [("/*", "*/")], ts: "tsx"),
        P("rust", "Rust", ["rs"], line: L("//"), block: [("/*", "*/")], ts: "rust", nested: true),
        P("go", "Go", ["go"], line: L("//"), block: [("/*", "*/")], ts: "go"),
        P("swift", "Swift", ["swift"], line: L("//"), block: [("/*", "*/")], nested: true),
        P("kotlin", "Kotlin", ["kt", "kts"], line: L("//"), block: [("/*", "*/")], strings: PythonStrings, nested: true),
        P("scala", "Scala", ["scala", "sc"], line: L("//"), block: [("/*", "*/")], strings: PythonStrings, ts: "scala"),
        P("groovy", "Groovy", ["groovy", "gvy", "gy", "gradle"], line: L("//", "#"), block: [("/*", "*/")], shebang: "#", heredocs: true),
        P("objc", "Objective-C", ["m", "mm"], line: L("//"), block: [("/*", "*/")]),
        P("php", "PHP", ["php", "php3", "php4", "php5", "phtml"], line: L("//", "#"), block: [("/*", "*/")], shebang: "#", ts: "php", heredocs: true),
        P("python", "Python", ["py", "pyw", "pyi"], line: L("#"), strings: PythonStrings, ts: "python", shebang: "#", heredocs: true),
        P("ruby", "Ruby", ["rb", "rake", "gemspec", "rbw"], line: L("#"), block: [("=begin", "=end")], shebang: "#", ts: "ruby", heredocs: true),
        P("perl", "Perl", ["pl", "pm", "t"], line: L("#"), shebang: "#", heredocs: true),
        P("shell", "Shell", ["sh", "bash", "zsh", "ksh", "csh", "tcsh", "fish"], line: L("#"), strings: ShellStrings, ts: "bash", shebang: "#"),
        P("powershell", "PowerShell", ["ps1", "psm1", "psd1"], line: L("#"), block: [("<#", "#>")], strings: ShellStrings),
        P("r", "R", ["r"], line: L("#"), shebang: "#"),
        P("julia", "Julia", ["jl"], line: L("#"), block: [("#=", "=#")], strings: PythonStrings, ts: "julia", shebang: "#", nested: true),
        P("lua", "Lua", ["lua"], line: L("--"), block: [("--[[", "]]")], strings: PythonStrings),
        P("haskell", "Haskell", ["hs", "lhs"], line: L("--"), block: [("{-", "-}")], ts: "haskell", nested: true),
        P("ocaml", "OCaml", ["ml", "mli"], block: [("(*", "*)")], ts: "ocaml", nested: true),
        P("fsharp", "F#", ["fs", "fsi", "fsx"], line: L("//"), block: [("(*", "*)")], strings: PythonStrings, nested: true),
        P("elm", "Elm", ["elm"], line: L("--"), block: [("{-", "-}")], strings: PythonStrings, nested: true),
        P("erlang", "Erlang", ["erl", "hrl"], line: L("%")),
        P("elixir", "Elixir", ["ex", "exs"], line: L("#"), strings: PythonStrings, shebang: "#"),
        P("clojure", "Clojure", ["clj", "cljs", "cljc"], line: L(";"), strings: LispStrings),
        P("lisp", "Lisp", ["lisp", "lsp", "cl"], line: L(";"), block: [("#|", "|#")], strings: LispStrings, nested: true),
        P("scheme", "Scheme", ["scm", "ss", "sld", "sls"], line: L(";"), block: [("#|", "|#")], strings: LispStrings, nested: true),
        P("racket", "Racket", ["rkt", "rktd"], line: L(";"), block: [("#|", "|#")], strings: LispStrings, nested: true),
        P("elisp", "Emacs Lisp", ["el"], line: L(";"), strings: LispStrings),
        P("sql", "SQL", ["sql"], line: L("--"), block: [("/*", "*/")], strings: SqlStrings),
        P("postgres", "PostgreSQL", ["pgsql"], line: L("--"), block: [("/*", "*/")], strings: SqlStrings),
        P("tsql", "T-SQL", ["tsql"], line: L("--"), block: [("/*", "*/")], strings: SqlStrings),
        P("html", "HTML", ["html", "htm", "xhtml", "shtml"], block: [("<!--", "-->")], strings: HtmlStrings, ts: "html"),
        P("xml", "XML", ["xml", "xsd", "xsl", "xslt", "xaml", "svg", "nuspec", "props", "targets", "pubxml", "csproj", "vbproj", "fsproj", "proj", "config", "resx", "wxs", "wxi"], block: [("<!--", "-->")], strings: HtmlStrings),
        P("css", "CSS", ["css"], block: [("/*", "*/")], strings: HtmlStrings, ts: "css"),
        P("scss", "SCSS", ["scss"], line: L("//"), block: [("/*", "*/")], strings: HtmlStrings),
        P("less", "Less", ["less"], line: L("//"), block: [("/*", "*/")], strings: HtmlStrings),
        P("sass", "Sass", ["sass"], line: L("//"), block: [("/*", "*/")], strings: HtmlStrings),
        P("stylus", "Stylus", ["styl"], line: L("//"), block: [("/*", "*/")], strings: HtmlStrings),
        P("json", "JSON", ["json", "jsonc", "json5", "geojson", "topojson"], line: L("//"), block: [("/*", "*/")], ts: "json"),
        P("yaml", "YAML", ["yaml", "yml"], line: L("#"), strings: ShellStrings),
        P("toml", "TOML", ["toml"], line: L("#"), strings: PythonStrings, ts: "toml"),
        P("ini", "INI", ["ini", "cfg", "conf", "properties"], line: L("#", ";")),
        P("markdown", "Markdown", ["md", "markdown", "mdx"], block: [("<!--", "-->")]),
        P("dockerfile", "Dockerfile", ["dockerfile"], fileNames: ["Dockerfile"], line: L("#")),
        P("makefile", "Makefile", ["mk"], fileNames: ["Makefile", "makefile", "GNUmakefile"], line: L("#")),
        P("cmake", "CMake", ["cmake"], fileNames: ["CMakeLists.txt"], line: L("#")),
        P("batch", "Batch", ["bat", "cmd"], line: LS("REM", "::"), strings: BackslashStrings),
        P("vbscript", "VBScript", ["vbs"], line: L("'"), strings: VbStrings),
        P("visualbasic", "Visual Basic", ["vb"], line: L("'"), strings: VbStrings),
        P("vba", "VBA", ["bas", "cls", "frm"], line: L("'"), strings: VbStrings),
        P("pascal", "Pascal", ["pas", "pp", "p", "dpr", "dpk"], line: L("//"), block: [("{", "}"), ("(*", "*)")], strings: PascalStrings),
        P("ada", "Ada", ["adb", "ads"], line: L("--"), strings: VbStrings),
        P("fortran", "Fortran", ["f", "f90", "f95", "f03", "f08", "for", "ftn"], line: L("!"), strings: FortranStrings),
        P("cobol", "COBOL", ["cob", "cbl", "cpy"], line: L("*>")),
        P("asm", "Assembly (NASM)", ["asm", "nasm"], line: L(";")),
        P("gas", "Assembly (GNU)", ["s", "S"], line: L("#", "//", "@"), block: [("/*", "*/")]),
        P("verilog", "Verilog", ["v", "vh"], line: L("//"), block: [("/*", "*/")], ts: "verilog"),
        P("systemverilog", "SystemVerilog", ["sv", "svh"], line: L("//"), block: [("/*", "*/")], ts: "verilog"),
        P("vhdl", "VHDL", ["vhd", "vhdl"], line: L("--")),
        P("tcl", "Tcl", ["tcl"], line: L("#"), strings: BackslashStrings),
        P("awk", "AWK", ["awk"], line: L("#"), strings: SlashStrings),
        P("coffeescript", "CoffeeScript", ["coffee"], line: L("#"), block: [("###", "###")], strings: PythonStrings),
        P("haml", "Haml", ["haml"], line: L("-#")),
        P("slim", "Slim", ["slim"], line: L("/#")),
        P("pug", "Pug", ["pug", "jade"], line: L("//"), block: [("/*", "*/")]),
        P("latex", "LaTeX", ["tex", "sty", "cls", "ltx"], line: L("%")),
        P("matlab", "MATLAB", ["m"], line: L("%"), block: [("%{", "%}")], strings: PascalStrings),
        P("prolog", "Prolog", ["pro", "prolog"], line: L("%"), block: [("/*", "*/")], strings: SqlStrings),
        P("d", "D", ["d", "di"], line: L("//"), block: [("/*", "*/"), ("/+", "+/")], nested: true),
        P("nim", "Nim", ["nim"], line: L("#"), block: [("#[", "]#")], strings: SlashStrings, nested: true),
        P("crystal", "Crystal", ["cr"], line: L("#"), shebang: "#", heredocs: true),
        P("zig", "Zig", ["zig"], line: L("//")),
        P("v", "V", ["v"], line: L("//"), block: [("/*", "*/")], strings: PascalStrings),
        P("vala", "Vala", ["vala", "vapi"], line: L("//"), block: [("/*", "*/")]),
        P("cython", "Cython", ["pyx", "pxd", "pxi"], line: L("#"), strings: PythonStrings, shebang: "#"),
        P("gdscript", "GDScript", ["gd"], line: L("#"), shebang: "#"),
        P("solidity", "Solidity", ["sol"], line: L("//"), block: [("/*", "*/")]),
        P("vyper", "Vyper", ["vy"], line: L("#"), strings: PythonStrings),
        P("nix", "Nix", ["nix"], line: L("#"), block: [("/*", "*/")], strings: PythonStrings),
        P("haxe", "Haxe", ["hx", "hxml"], line: L("//"), block: [("/*", "*/")]),
        P("autohotkey", "AutoHotkey", ["ahk", "ahkl"], line: L(";"), block: [("/*", "*/")]),
        P("purescript", "PureScript", ["purs"], line: L("--"), block: [("{-", "-}")], strings: SlashStrings),
        P("idris", "Idris", ["idr", "lidr"], line: L("--"), block: [("{-", "-}")]),
        P("agda", "Agda", ["agda", "lagda"], line: L("--"), block: [("{-", "-}")], ts: "agda", nested: true),
        P("dart", "Dart", ["dart"], line: L("//"), block: [("/*", "*/")], strings: PythonStrings),
        P("processing", "Processing", ["pde"], line: L("//"), block: [("/*", "*/")]),
        P("arduino", "Arduino", ["ino"], line: L("//"), block: [("/*", "*/")], ts: "c"),
        P("reason", "Reason", ["re", "rei"], line: L("//"), block: [("/*", "*/")]),
        P("hcl", "HCL", ["hcl", "tf", "tfvars"], line: L("#", "//"), block: [("/*", "*/")], strings: BackslashStrings),
        P("proto", "Protocol Buffers", ["proto"], line: L("//"), block: [("/*", "*/")]),
        P("graphql", "GraphQL", ["graphql", "gql"], line: L("#"), strings: ShellStrings),
        P("thrift", "Thrift", ["thrift"], line: L("//", "#"), block: [("/*", "*/")]),
        P("apacheconf", "Apache Conf", ["htaccess"], line: L("#"), strings: ShellStrings),
        P("vue", "Vue", ["vue"], block: [("<!--", "-->")]),
        P("svelte", "Svelte", ["svelte"], block: [("<!--", "-->")]),
        P("erb", "ERB", ["erb", "rhtml"], block: [("<%#", "%>")], ts: "embedded-template"),
        P("ejs", "EJS", ["ejs"], block: [("<%#", "%>")], ts: "embedded-template"),
        P("razor", "Razor", ["cshtml", "razor"], block: [("@*", "*@"), ("<!--", "-->")], ts: "razor"),
        P("handlebars", "Handlebars", ["hbs", "handlebars", "mustache"], block: [("{{!--", "--}}"), ("{{!", "}}")]),
        P("twig", "Twig", ["twig"], block: [("{#", "#}")]),
        P("jinja", "Jinja", ["jinja", "j2", "j2d"], block: [("{#", "#}")]),
        P("qml", "QML", ["qml"], line: L("//"), block: [("/*", "*/")]),
        P("glsl", "GLSL", ["glsl", "vert", "frag", "geom", "tesc", "tese", "comp"], line: L("//"), block: [("/*", "*/")]),
        P("hlsl", "HLSL", ["hlsl", "hlsli"], line: L("//"), block: [("/*", "*/")]),
        P("cg", "Cg", ["cg", "cgfx"], line: L("//"), block: [("/*", "*/")]),
        P("opencl", "OpenCL", ["cl"], line: L("//"), block: [("/*", "*/")]),
        P("cuda", "CUDA", ["cu", "cuh"], line: L("//"), block: [("/*", "*/")], ts: "c"),
        P("starlark", "Starlark", ["bzl"], fileNames: ["BUILD", "BUILD.bazel", "WORKSPACE"], line: L("#")),
        P("meson", "Meson", ["meson"], fileNames: ["meson.build"], line: L("#")),
        P("jenkinsfile", "Jenkinsfile", [], fileNames: ["Jenkinsfile"], line: L("//"), block: [("/*", "*/")]),
        P("gemfile", "Gemfile", [], fileNames: ["Gemfile"], line: L("#")),
        P("rakefile", "Rakefile", [], fileNames: ["Rakefile"], line: L("#")),
        P("vagrantfile", "Vagrantfile", [], fileNames: ["Vagrantfile"], line: L("#")),
        P("dotenv", "Env", [], fileNames: [".env", ".env.local", ".env.example"], line: L("#")),
        P("gitignore", "GitIgnore", [], fileNames: [".gitignore", ".dockerignore", ".npmignore", ".eslintignore"], line: L("#")),
        P("editorconfig", "EditorConfig", [], fileNames: [".editorconfig"], line: L("#", ";")),
        P("requirements", "Requirements", [], fileNames: ["requirements.txt"], line: L("#")),
        P("t4", "T4 Template", ["tt", "t4"], line: L("//"), block: [("/*", "*/")]),
        P("aspx", "ASP.NET", ["aspx", "ascx", "master", "ashx", "asax"], block: [("<!--", "-->")]),
    };
}
