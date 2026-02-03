# AddyScript lexer for pygments

from pygments.lexer import RegexLexer, words, include
from pygments.token import (
    Text, Comment, Operator, Keyword, Name, String,
    Number, Punctuation
)

class AddyScriptLexer(RegexLexer):
    """
    AddyScript lexer for Pygments.
    """

    name = "AddyScript"
    aliases = ["addyscript", "add"]
    filenames = ["*.add"]
    mimetypes = ["text/x-addyscript"]

    tokens = {

        "root": [
            include("whitespace"),
            include("comments"),
            include("keywords"),
            include("types"),
            include("numbers"),
            include("strings"),
            include("operators"),
            include("punctuation"),
            include("identifiers"),
        ],

        # --------------------
        # Whitespace
        # --------------------
        "whitespace": [
            (r"\s+", Text),
        ],

        # --------------------
        # Comments
        # --------------------
        "comments": [
            (r"//.*?$", Comment.Single),
            (r"/\*", Comment.Multiline, "comment"),
        ],

        "comment": [
            (r"\*/", Comment.Multiline, "#pop"),
            (r"[^*/]+", Comment.Multiline),
            (r"[*/]", Comment.Multiline),
        ],

        # --------------------
        # Keywords
        # --------------------
        "keywords": [
            (words((
                "let", "abstract", "and", "as", "break", "case", "catch",
                "class", "const", "constructor", "contains", "continue",
                "default", "do", "else", "endswith", "event", "extern",
                "final", "finally", "for", "foreach", "function", "goto",
                "if", "import", "in", "is", "matches", "new", "not",
                "operator", "or", "private", "property", "protected",
                "public", "read", "record", "return", "startswith",
                "static", "super", "switch", "this", "throw", "try",
                "typeof", "var", "when", "while", "with", "write", "yield"
            ), suffix=r"\b"), Keyword),

            (words((
                "__context", "__key", "__value", "false", "null", "true"
            ), suffix=r"\b"), Keyword.Constant),
        ],

        # --------------------
        # Built-in Types
        # --------------------
        "types": [
            (words((
                "blob", "bool", "closure", "complex", "date", "decimal",
                "duration", "float", "int", "list", "long", "map", "object",
                "queue", "rational", "resource", "set", "stack", "string",
                "tuple", "void"
            ), suffix=r"\b"), Keyword.Type),
        ],

        # --------------------
        # Numbers
        # --------------------
        "numbers": [
            (r"0b[01_]+", Number.Bin),
            (r"0x[0-9a-fA-F_]+", Number.Hex),
            (r"\d[\d_]*\.\d[\d_]*(e[+-]?\d+)?", Number.Float),
            (r"\d[\d_]*", Number.Integer),
        ],

        # --------------------
        # Strings / Chars
        # --------------------
        "strings": [
            (r"\$@'", String.Interpol, "interpolated-verbatim-char"),
            (r"\$@\"", String.Interpol, "interpolated-verbatim-string"),
            (r"\$'", String.Interpol, "interpolated-char"),
            (r'\$"', String.Interpol, "interpolated-string"),
            (r"@'", String, "verbatim-char"),
            (r'@"', String, "verbatim-string"),
            (r"'", String, "char"),
            (r'"', String, "string"),
            (r'`[^`]+`', String.Char),
        ],

        "string": [
            (r'"', String, "#pop"),
            (r'\\["\\abfnrtv0]', String.Escape),
            (r'[^"\\]+', String),
            (r'.', String),
        ],

        "verbatim-string": [
            (r'""', String),
            (r'"', String, "#pop"),
            (r'[^"]+', String),
        ],

        "char": [
            (r"'", String, "#pop"),
            (r"\\['\\abfnrtv0]", String.Escape),
            (r"[^'\\]+", String),
            (r".", String),
        ],

        "verbatim-char": [
            (r"''", String),
            (r"'", String, "#pop"),
            (r"[^']+", String),
        ],

        # --------------------
        # Interpolated Strings
        # --------------------
        "interpolated-string": [
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r'"', String.Interpol, "#pop"),
            (r'\\["\\abfnrtv0]', String.Escape),
            (r'[^"\\{]+', String.Interpol),
            (r'.', String.Interpol),
        ],

        "interpolated-verbatim-string": [
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r'""', String),
            (r'"', String.Interpol, "#pop"),
            (r'[^"{]+', String.Interpol),
        ],

        "interpolated-char": [
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r"'", String.Interpol, "#pop"),
            (r"\\['\\abfnrtv0]", String.Escape),
            (r"[^'\\]+", String.Interpol),
            (r".", String.Interpol),
        ],

        "interpolated-verbatim-char": [
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r"''", String),
            (r"'", String.Interpol, "#pop"),
            (r"[^'{]+", String.Interpol),
        ],

        # --------------------
        # Interpolation Expression
        # --------------------
        "interpolation": [
            include("whitespace"),
            include("comments"),
            include("keywords"),
            include("types"),
            include("numbers"),
            include("strings"),
            include("operators"),
            include("punctuation"),
            include("identifiers"),
            (r'}', String.Interpol, "#pop"),
        ],

        # --------------------
        # Operators
        # --------------------
        "operators": [
            (r"[+\-*/%&|\^!~=<>?:]+", Operator),
        ],

        # --------------------
        # Punctuation
        # --------------------
        "punctuation": [
            (r"[()\[\]{},.;]", Punctuation),
        ],

        # --------------------
        # Identifiers
        # --------------------
        "identifiers": [
            (r"[A-Za-z_]\w*", Name),
        ],
    }
