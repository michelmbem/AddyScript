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
                "__context", "__key", "__value", "abstract", "and", "as",
                "blob", "bool", "break", "case", "catch", "class", "closure",
                "complex", "const", "constructor", "contains", "continue",
                "date", "decimal", "default", "do", "duration", "else",
                "endswith", "event", "extern", "false", "final", "finally",
                "float", "for", "foreach", "function", "goto", "if", "import",
                "in", "int", "is", "let", "list", "long", "map", "matches",
                "new", "not", "null", "object", "operator", "or", "private",
                "property", "protected", "public", "queue", "rational", "read",
                "record", "resource", "return", "set", "stack", "startswith",
                "static", "string", "super", "switch", "this", "throw", "true",
                "try", "tuple", "typeof", "var", "void", "when", "while", "with",
                "write", "yield"
            ), prefix=r"\b", suffix=r"\b"), Keyword),
        ],
    }
