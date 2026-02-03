from mkdocs.plugins import BasePlugin

class LoadAddyScriptLexerPlugin(BasePlugin):
    def on_config(self, config):
        # Import registers the lexer with Pygments
        import as_lexer  # noqa
        return config
