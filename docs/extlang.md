# Extending the language syntax

### Creating custom statements

Creating a custom statement can be summarized as follows:

1. Create a (direct or indirect) subclass of the _AddyScript.Ast.AstNode_ class (moreover, you will subclass _AddyScript.Ast.Statements.Statement_ or _AddyScript.Ast.Expressions.Expression_).

2. Override the _AcceptTranslator_ method of your class; this may require the addition of a new method to the _AddyScript.Translators.ITranslator_ interface (e.g.: if your class is called _Unless_, the body of its _AcceptTranslator_ method could be something like `translator.TranslateUnless(this);` thus requiring the addition of a _TranslateUnless_ method in the _ITranslator_ interface).

3. Create a stub for the eventually newly created method in any class implementing the _ITranslator_ interface.

4. If your statement introduces new terminal symbols in the language (like an **unless** keyword), add corresponding members to the _AddyScript.Parsers.TokenID_ enumeration (e.g.: you could add a _KW_Unless_ member representing the **unless** keyword). Update the _AddyScript.Parsers.Token_ class to take your tokens into account in some of its method (like the static version of _ToString_). Change the _NextToken_ method of the _AddyScript.Parsers.Lexer_ class to recognize your tokens. For a keyword, you'll just have to add it to the keywords registry. To do so, open the _AddyScript.Parsers.Keyword.cs_ source file, find the class initializer and add a line of code to it like the following one: `Register("unless", TokenID.KW_Unless);`.

5. Update the _AddyScript.Parsers.Parser_ class to recognize your statement (or the _AddyScript.Parsers.ExpressionParser_ class if you are creating a new kind of expression). This is typically done in two stages: first, find the _Statement_ method and add a case label for your statement (something like: `case TokenID.KW_Unless: return Unless();`). secondly, define the method that recognizes your statement.

6. Recognizing a statement is somehow straightforward. Assuming that your statement is a sequence of terminal and non terminal symbols, you just have to ensure that each of those symbols appears at the right place. To ensure that a terminal symbol (a token) is where it should be, use the _Match_ method of the parser. The _TryMatch_ method does the same job except that it does not throw an exception when the expected token is not matched. Use it for optional tokens. There are also variants of _Match_ and _TryMatch_ (respectively _MatchAny_ and _TryMatchAny_) which accept several tokens and try to match any of them. For a non-terminal symbol, use the method that recognizes that symbol; such a method generally has the same name than the corresponding symbol, expects no parameter and returns the recognized non-terminal symbol upon completion. Don't forget to set the location of your symbol in the source file upon completion. So recognizing the unless-statement could be done like this:

    ```CSharp
    protected Unless Unless()
    {
        // Trying to recognize: unless (expr) stmt

        // We first match the unless keyword
        Token first = Match(TokenID.KW_Unless);

        // Then we match the left parenthesis
        Match(TokenID.LeftParenthesis);
        // We call the method that recognizes expressions and store the result in expr
        var expr = Expression();
        // We match the right parenthesis
        Match(TokenID.RightParenthesis);
        
        // We call the method that recognizes statements and store the result in stmt
        var stmt = Statement();
        
        // We create an instance of Unless expr and stmt as child nodes and return it
        var unless = new Unless(expr, stmt);
        unless.SetLocation(first.Start, stmt.End);
        return unless;
    }
    ```

7. The _BasicParser_ class provides some helper methods to recognize a sequence of non-terminal symbols of the same kind or a non-null instance of some symbol.

8. Once you've recognized your statement, the next stage is to interpret it. It's up to you to define the logic of your statement. But an unless-statement would probably be similar to an if-statement, so the _TranslateUnless_ method of the _Interpreter_ class could look like this:

    ```CSharp
   public void TranslateUnless(Unless unless)
   {
      if (!IsTrue(unless.Condition))
         unless.Body.AccepCompiler(this);
   }
   ```

That's all!

### Going further

Well, to go further in AddyScript extension, just dive into the source code and discover how all the engine works. Don't forget to share your experience with us.

[Home](README.md) | [Previous](extapi.md) | [Next](improve.md)