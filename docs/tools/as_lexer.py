# AddyScript lexer for pygments

from pygments.lexer import RegexLexer, words, include
from pygments.token import (
    Text, Comment, Operator, Keyword, Name, String,
    Number, Punctuation, Literal
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
            (r"//.*$", Comment.Single),
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
                "let", "abstract", "as", "break", "case", "catch",
                "class", "const", "constructor", "continue", "default",
                "do", "else", "event", "extern", "final", "finally",
                "for", "foreach", "function", "goto", "if", "import",
                "operator", "private", "property", "protected", "public",
                "record", "return", "static", "super", "switch", "this",
                "throw", "try", "var", "when", "while", "yield",
            ), suffix=r"\b"), Keyword),

            (words((
                "__context", "__key", "__value", "read", "write",
            ), suffix=r"\b"), Keyword.Pseudo),

            (words((
                "false", "null", "true",
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
                "tuple", "void",
            ), suffix=r"\b"), Keyword.Type),
        ],

        # --------------------
        # Numbers
        # --------------------
        "numbers": [
            (r"0b[01_]+", Number.Bin),
            (r"0x[0-9a-fA-F_]+", Number.Hex),
            (r"\d[\d_]*\.\d[\d_]*([eE][+-]?\d[\d_]*)?", Number.Float),
            (r"\.\d[\d_]*([eE][+-]?\d[\d_]*)?", Number.Float),
            (r"\d[\d_]*[eE][+-]?\d[\d_]*", Number.Float),
            (r"\d[\d_]*", Number.Integer),
        ],

        # --------------------
        # Strings
        # --------------------
        "strings": [
            (r"@'", String, "verbatim-string-single"),
            (r'@"', String, "verbatim-string-double"),
            (r"'", String, "string-single"),
            (r'"', String, "string-double"),
            (r'`[^`]*`', Literal.Date),
        ],

        "string-single": [
            (r"'", String, "#pop"),
            (r"\\['\\abfnrtv0]", String.Escape),
            (r"[^'\\]+", String),
            (r'.', String),
        ],

        "string-double": [
            (r'"', String, "#pop"),
            (r'\\["\\abfnrtv0]', String.Escape),
            (r'[^"\\]+', String),
            (r'.', String),
        ],

        "verbatim-string-single": [
            (r"''", String),
            (r"'", String, "#pop"),
            (r"[^']+", String),
            (r'.', String),
        ],

        "verbatim-string-double": [
            (r'""', String),
            (r'"', String, "#pop"),
            (r'[^"]+', String),
            (r'.', String),
        ],

        # --------------------
        # Operators
        # --------------------
        "operators": [
            (r"[+\-*/%&|\^!~=<>?:]+", Operator),

            (words((
                "and", "contains", "endswith", "in", "is", "matches",
                "new", "not", "or", "startswith", "typeof", "with",
            ), suffix=r"\b"), Operator.Word),
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
            (r"[A-Za-z_]\w*(?=\s*\())", Name.Function),
            (r"[A-Za-z_]\w*", Name),
        ],
    }
