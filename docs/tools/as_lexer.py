# AddyScript lexer for pygments

from pygments.lexer import RegexLexer, words, include
from pygments.token import (
    Comment, String, Literal, Keyword, Name,
    Operator, Number, Punctuation
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
            include("comments"),
            include("strings"),
            include("keywords"),
            include("identifiers"),
            include("numbers"),
            include("operators"),
            include("punctuation"),
        ],

        # --------------------
        # Comments
        # --------------------
        "comments": [
            (r'/\*', Comment.Multiline, 'comment'),
            (r'//.*?$', Comment.Singleline),
        ],

        "comment": [
            (r'[^*/]', Comment.Multiline),
            (r'/\*', Comment.Multiline, '#push'),
            (r'\*/', Comment.Multiline, '#pop'),
            (r'[*/]', Comment.Multiline)
        ],

        # --------------------
        # Strings
        # --------------------
        "strings": [
            (r"\$@'", String, "interpolated-verbatim-string-single"),
            (r'\$@"', String, "interpolated-verbatim-string-double"),
            (r"\$'", String, "interpolated-string-single"),
            (r'\$"', String, "interpolated-string-double"),
            (r"@'", String, "verbatim-string-single"),
            (r'@"', String, "verbatim-string-double"),
            (r"'", String, "string-single"),
            (r'"', String, "string-double"),
            (r'`[^`]*`', Literal.Date),
        ],

        "string-single": [
            (r"'", String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{[^}]*}', String.Escape),
            (r"\\['\\abfnrtv0]", String.Escape),
            (r"[^'\\{}]+", String),
            (r'.', String),
        ],

        "string-double": [
            (r'"', String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{[^}]*}', String.Escape),
            (r'\\["\\abfnrtv0]', String.Escape),
            (r'[^"\\{}]+', String),
            (r'.', String),
        ],

        "verbatim-string-single": [
            (r"''", String),
            (r"'", String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{[^}]*}', String.Escape),
            (r"\\['\\abfnrtv0]", String.Escape),
            (r"[^'{}]+", String),
            (r'.', String),
        ],

        "verbatim-string-double": [
            (r'""', String),
            (r'"', String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{[^}]*}', String.Escape),
            (r'\\["\\abfnrtv0]', String.Escape),
            (r'[^"{}]+', String),
            (r'.', String),
        ],

        # --------------------
        # Interpolated Strings
        # --------------------
        "interpolated-string-single": [
            (r"'", String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r"\\['\\abfnrtv0]", String.Escape),
            (r"[^'{}]+", String),
            (r'.', String),
        ],

        "interpolated-string-double": [
            (r'"', String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r'\\["\\abfnrtv0]', String.Escape),
            (r'[^"{}]+', String),
            (r'.', String),
        ],

        "interpolated-verbatim-string-single": [
            (r"''", String),
            (r"'", String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r"\\['\\abfnrtv0]", String.Escape),
            (r"[^'{}]+", String),
            (r'.', String),
        ],

        "interpolated-verbatim-string-double": [
            (r'""', String),
            (r'"', String, "#pop"),
            (r'{{', String.Escape),
            (r'}}', String.Escape),
            (r'{', String.Interpol, "interpolation"),
            (r'\\["\\abfnrtv0]', String.Escape),
            (r'[^"{}]+', String),
            (r'.', String),
        ],
        
        # --------------------
        # Interpolation Expression
        # --------------------
        "interpolation": [
            (r'}', String.Interpol, "#pop"),

            # string literals INSIDE interpolation
            (r"'", String, "string-single"),
            (r'"', String, "string-double"),
            (r"@'", String, "verbatim-string-single"),
            (r'@"', String, "verbatim-string-double"),

            include("keywords"),
            include("identifiers"),
            include("numbers"),
            include("operators"),
            include("punctuation"),
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
            ), prefix=r"\b", suffix=r"\b"), Keyword),

            (words((
                "__context", "__key", "__value", "read", "write",
            ), prefix=r"\b", suffix=r"\b"), Keyword.Pseudo),

            (words((
                "false", "null", "true",
            ), prefix=r"\b", suffix=r"\b"), Keyword.Constant),

            (words((
                "blob", "bool", "closure", "complex", "date", "decimal",
                "duration", "float", "int", "list", "long", "map", "object",
                "queue", "rational", "resource", "set", "stack", "string",
                "tuple", "void",
            ), prefix=r"\b", suffix=r"\b"), Keyword.Type),

            (words((
                "and", "contains", "endswith", "in", "is", "matches",
                "new", "not", "or", "startswith", "typeof", "with",
            ), prefix=r"\b", suffix=r"\b"), Operator.Word),
        ],

        # --------------------
        # Identifiers
        # --------------------
        "identifiers": [
            (r"[A-Za-z_]\w*(?=\s*\()", Name.Function),
            (r"\$?[A-Za-z_]\w*", Name),
        ],

        # --------------------
        # Numbers
        # --------------------
        "numbers": [
            (r"0b[01_]+[lfdi]?", Number.Bin),
            (r"0x[0-9a-fA-F_]+[lLfFdDiI]?", Number.Hex),
            (r"\d[\d_]*\.\d[\d_]*([eE][+-]?\d[\d_]*)?[fFdDiI]?", Number.Float),
            (r"\.\d[\d_]*([eE][+-]?\d[\d_]*)?[fFdDiI]?", Number.Float),
            (r"\d[\d_]*[eE][+-]?\d[\d_]*[fFdDiI]?", Number.Float),
            (r"\d[\d_]*[lLfFdDiI]?", Number.Integer),
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
    }
