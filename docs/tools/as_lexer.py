# AddyScript lexer for pygments

from pygments.lexer import RegexLexer, words, include
from pygments.token import Comment, Keyword

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
            include("keywords"),
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
        ],
    }
