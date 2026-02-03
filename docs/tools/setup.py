from setuptools import setup

setup(
    name="addyscript-lexer",
    py_modules=["addyscript_lexer"],
    entry_points={
        'pygments.lexers': [
            'AddyScript = addyscript_lexer:AddyScriptLexer',
            'addyscript = addyscript_lexer:AddyScriptLexer',
            'add = addyscript_lexer:AddyScriptLexer',
        ],
    },
)
