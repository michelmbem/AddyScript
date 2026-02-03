from setuptools import setup

setup(
    name="addyscript-lexer",
    py_modules=["as_lexer"],
    entry_points={
        'pygments.lexers': [
            'AddyScript = as_lexer:AddyScriptLexer',
            'addyscript = as_lexer:AddyScriptLexer',
            'add = as_lexer:AddyScriptLexer',
        ],
    },
)
