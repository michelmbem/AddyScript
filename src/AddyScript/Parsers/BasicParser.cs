using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime.OOP;


namespace AddyScript.Parsers;


/// <summary>
/// The base class of all AddyScript parsers.<br/>
/// A collection of utility methods for derived classes.
/// </summary>
public abstract class BasicParser
{
    #region Fields

    private const int MAX_BUFFER_SIZE = 100;
    private const string MAIN_FUNCTION_NAME = "main";
    private const string ITERATOR_FUNCTION_NAME = "iterator";

    private readonly Lexer lexer;
    private readonly List<Token> buffer = [];
    private int tokenIndex;

    protected Token token;

    #endregion

    #region Constructor and properties

    /// <summary>
    /// Initializes a new instance of BaseParser
    /// </summary>
    /// <param name="lexer">The bound lexer</param>
    protected BasicParser(Lexer lexer)
    {
        // Checks if lexer is null
        ArgumentNullException.ThrowIfNull(lexer);

        // Initializes the lexer and peeks the first token from it.
        this.lexer = lexer;
        token = Ll(1);

        // Stores the default 'main' function on top of the stack
        PushFunction(MAIN_FUNCTION_NAME, false, false, false);
    }

    /// <summary>
    /// Gets the name of the source file that's being parsed.
    /// </summary>
    public string FileName => lexer.FileName;

    /// <summary>
    /// Gets or sets the currently parsed class definition.
    /// </summary>
    protected ParseTimeClass CurrentClass { get; set; }

    /// <summary>
    /// Gets or sets the currently parsed function or method definition.
    /// </summary>
    protected ParseTimeFunction CurrentFunction { get; set; }

    #endregion

    #region Utility

    /// <summary>
    /// Gets a token without removing it from the buffer.
    /// </summary>
    /// <param name="k">The relative index of the token to peek</param>
    /// <returns>A <see cref="Token"/></returns>
    protected Token Ll(int k)
    {
        Trace.Assert(k > 0);

        int realIndex = tokenIndex + k;

        while (buffer.Count < realIndex)
            buffer.Add(lexer.NextToken());

        return buffer[realIndex - 1];
    }

    /// <summary>
    /// Skips a number of tokens and peeks the next from the lexer.
    /// </summary>
    /// <param name="count">The number of tokens to skip</param>
    protected void Consume(int count)
    {
        Trace.Assert(count > 0 && tokenIndex + count <= buffer.Count);

        tokenIndex += count;
        token = Ll(1);

        while (tokenIndex >= MAX_BUFFER_SIZE)
        {
            buffer.RemoveRange(0, MAX_BUFFER_SIZE);
            tokenIndex -= MAX_BUFFER_SIZE;
        }
    }

    /// <summary>
    /// Skips successive comments until a <i>non-comment</i> token is encountered.
    /// </summary>
    protected void SkipComments()
    {
        while (token.IsComment) Consume(1);
    }

    /// <summary>
    /// Requires the next <see cref="Token"/> that is not a comment to satisfy a particular <paramref name="predicate"/>.<br/>
    /// Throws a <see cref="SyntaxError"/> if it doesn't.<br/>
    /// Consumes the <see cref="Token"/> otherwise.
    /// </summary>
    /// <param name="predicate">The <see cref="Predicate{Token}"/> that the next non-comment <see cref="Token"/> should satisfy</param>
    /// <returns>The matched <see cref="Token"/></returns>
    protected Token Match(Predicate<Token> predicate)
    {
        SkipComments();

        if (!predicate(token))
            throw new SyntaxError(FileName, token);

        Token matched = token;
        Consume(1);
        return matched;
    }

    /// <summary>
    /// Requires the next <see cref="Token"/> that is not a comment to have the required <see cref="TokenID"/>.<br/>
    /// Throws a <see cref="SyntaxError"/> if it doesn't.<br/>
    /// Consumes the <see cref="Token"/> otherwise.
    /// </summary>
    /// <param name="requiredID">The <see cref="TokenID"/> that the next non-comment <see cref="Token"/> should have</param>
    /// <returns>The matched <see cref="Token"/></returns>
    protected Token Match(TokenID requiredID) => Match(t => t.TokenID == requiredID);

    /// <summary>
    /// Requires the next <see cref="Token"/> that is not a comment to have one of the required <see cref="TokenID"/>s.<br/>
    /// Throws a <see cref="SyntaxError"/> if it doesn't.<br/>
    /// Consumes the <see cref="Token"/> otherwise.
    /// </summary>
    /// <param name="requiredIDs">The set of <see cref="TokenID"/>s to search in</param>
    /// <returns>The matched <see cref="Token"/></returns>
    protected Token MatchAny(params TokenID[] requiredIDs) =>
        Match(t => requiredIDs.Any(id => id == t.TokenID));

    /// <summary>
    /// Tests if the next <see cref="Token"/> that is not a comment satisfies a particular <paramref name="predicate"/>.<br/>
    /// </summary>
    /// <param name="predicate">The <see cref="Predicate{Token}"/> that the next non-comment <see cref="Token"/> could satisfy</param>
    /// <returns><b>true</b> if the <see cref="Token"/> satisfies the <paramref name="predicate"/>; <b>false</b> otherwise</returns>
    protected bool TryMatch(Predicate<Token> predicate)
    {
        SkipComments();
        return predicate(token);
    }

    /// <summary>
    /// Tests if the next <see cref="Token"/> that is not a comment has the given <see cref="TokenID"/>.
    /// </summary>
    /// <param name="requiredID">The <see cref="TokenID"/> we may want to match</param>
    /// <returns><b>true</b> if the token's ID matches the given <see cref="TokenID"/>; <b>false</b> otherwise</returns>
    protected bool TryMatch(TokenID requiredID) => TryMatch(t => t.TokenID == requiredID);

    /// <summary>
    /// Tests if the next <see cref="Token"/> that is not a comment has one of the given <see cref="TokenID"/>s.
    /// </summary>
    /// <param name="requiredIDs">The set of <see cref="TokenID"/>s to search in</param>
    /// <returns><b>true</b> if the token's ID matches one of the given <see cref="TokenID"/>s; <b>false</b> otherwise</returns>
    protected bool TryMatchAny(params TokenID[] requiredIDs) =>
        TryMatch(t => requiredIDs.Any(id => id == t.TokenID));

    /// <summary>
    /// Searches the <see cref="Token"/> stream for a symbol that is not a comment and satisfies a particular
    /// <paramref name="predicate"/> starting from the position that follows the current position.
    /// </summary>
    /// <param name="predicate">The <see cref="Predicate{Token}"/> that the next non-comment <see cref="Token"/> could satisfy</param>
    /// <param name="k">The position of the matched <see cref="Token"/> if any</param>
    /// <returns><b>true</b> if a matched <see cref="Token"/> is met ahead; <b>false</b> otherwise</returns>
    protected bool LookAhead(Predicate<Token> predicate, out int k)
    {
        k = 2;
        Token tmpTok = Ll(k);
        while (tmpTok.IsComment) tmpTok = Ll(++k);

        return predicate(tmpTok);
    }

    /// <summary>
    /// Searches the <see cref="Token"/> stream for a symbol that is not a comment and has the given <see cref="TokenID"/>
    /// starting from the position that follows the current position.
    /// </summary>
    /// <param name="lookupID">The <see cref="TokenID"/> to find</param>
    /// <param name="k">The position of the matched <see cref="Token"/> if any</param>
    /// <returns><b>true</b> if a matched <see cref="Token"/> is met ahead; <b>false</b> otherwise</returns>
    protected bool LookAhead(TokenID lookupID, out int k) =>
        LookAhead(t => t.TokenID == lookupID, out k);

    /// <summary>
    /// Executes some parsing method and verifies that the returned value is non-null.
    /// </summary>
    /// <typeparam name="T">The type of element to recognize</typeparam>
    /// <param name="syntaxRule">A reference to the parsing method</param>
    /// <param name="errorMessage">
    /// The message of the exception thrown whenever <paramref name="syntaxRule"/> returns null
    /// </param>
    /// <returns>A non-null instance of the desired type</returns>
    protected T Required<T>(Func<T> syntaxRule, string errorMessage) where T : ScriptElement =>
        syntaxRule() ?? throw new SyntaxError(FileName, token, errorMessage);

    /// <summary>
    /// Applies the asterisk operator to a parsing method.<br/>
    /// Recognizes a sequence of non-terminal symbols of the same type, zero or more times.
    /// </summary>
    /// <typeparam name="T">The type of non-terminal symbols to recognize</typeparam>
    /// <param name="syntaxRule">A reference to the parsing method used to recognize each symbol</param>
    /// <returns>An array of instances of the desired type</returns>
    protected T[] Asterisk<T>(Func<T> syntaxRule) where T : ScriptElement
    {
        List<T> elements = [];
        T element = syntaxRule();

        while (element != null)
        {
            elements.Add(element);
            element = syntaxRule();
        }

        return [.. elements];
    }

    /// <summary>
    /// Applies the plus rule to a parsing method.<br/>
    /// Recognizes a non-empty sequence of non-terminal symbols of the same type.
    /// </summary>
    /// <typeparam name="T">The type of non-terminal symbols to recognize</typeparam>
    /// <param name="syntaxRule">A reference to the parsing method used to recognize each symbol</param>
    /// <param name="errorMessage">The message of the exception thrown if the sequence is empty</param>
    /// <returns>A non-empty array of instances of the desired type</returns>
    protected T[] Plus<T>(Func<T> syntaxRule, string errorMessage) where T : ScriptElement
    {
        var elements = Asterisk(syntaxRule);
        return elements.Length > 0 ? elements : throw new SyntaxError(FileName, token, errorMessage);
    }

    /// <summary>
    /// Recognizes a comma-separated list of non-terminal symbols of the same type.
    /// </summary>
    /// <typeparam name="T">The type of non-terminal symbols to recognize</typeparam>
    /// <param name="syntaxRule">A reference to the parsing method used to recognize each symbol</param>
    /// <param name="checkUnicity">Tells if each symbol must be unique</param>
    /// <param name="errorMessage">The message of the exception thrown if a symbol is duplicated in the list</param>
    /// <returns>An array of instances of the desired type</returns>
    protected T[] List<T>(Func<T> syntaxRule, bool checkUnicity, string errorMessage) where T : ScriptElement
    {
        T element = syntaxRule();
        if (element == null) return [];

        List<T> elements = [element];

        while (TryMatch(TokenID.Comma))
        {
            Consume(1);

            element = Required(syntaxRule, Resources.AbnormalListTermination);
            if (checkUnicity && elements.Contains(element))
                throw new ScriptError(FileName, element, errorMessage);

            elements.Add(element);
        }

        return [.. elements];
    }

    #endregion

    #region Management of nested constructs

    /// <summary>
    /// Registers a class on top of the classes hierarchy.<br/>
    /// Even if, AddyScript does not support nesting classes it's
    /// still necessary to verify that certain symbols are not used
    /// out of a class's body.
    /// </summary>
    /// <param name="name">The name of the class to register</param>
    /// <param name="parent">The parent class's name</param>
    /// <param name="modifier">The class's modifier</param>
    protected void PushClass(Modifier modifier, string name, string parent)
    {
        CurrentClass = new ParseTimeClass(modifier, name, parent, CurrentClass);
    }

    /// <summary>
    /// Pops a class from the classes hierarchy.
    /// </summary>
    protected void PopClass()
    {
        CurrentClass = CurrentClass.Previous;
    }

    /// <summary>
    /// Registers a function on top of the functions hierarchy.
    /// </summary>
    /// <param name="name">The name of the function to register</param>
    /// <param name="isMethod">Tells if the function is a method</param>
    /// <param name="isContructor">Tells if the method is a constructor</param>
    /// <param name="isStatic">Tells if the method is static or not</param>
    protected void PushFunction(string name, bool isMethod, bool isContructor, bool isStatic)
    {
        CurrentFunction = new ParseTimeFunction(name, isMethod, isContructor, isStatic, CurrentFunction);
    }

    /// <summary>
    /// Pops a function from the functions hierarchy.
    /// </summary>
    protected void PopFunction()
    {
        CurrentFunction = CurrentFunction.Previous;
    }

    #endregion

    #region Nested types

    #region ParseTimeClass

    /// <summary>
    /// Represents a class at parse time.
    /// </summary>
    protected class ParseTimeClass(Modifier modifier, string name, string parentName, ParseTimeClass previous)
    {
        public Modifier Modifier => modifier;

        public string Name => name;

        public string ParentName => parentName;

        public ParseTimeClass Previous => previous;
    }

    #endregion

    #region ParseTimeFunction

    /// <summary>
    /// Represents a function at parse time.
    /// </summary>
    protected class ParseTimeFunction
    {
        public ParseTimeFunction(string name, bool isMethod, bool isContructor,
                                 bool isStatic, ParseTimeFunction previous)
        {
            Name = name;
            IsMethod = isMethod;
            IsContructor = isContructor;
            IsStatic = isStatic;
            Previous = previous;
            PushBlock(false);
        }

        public string Name { get; }

        public bool IsMethod { get; }

        public bool IsContructor { get; }

        public bool IsStatic { get; }

        public ParseTimeFunction Previous { get; }

        public ParseTimeBlock CurrentBlock { get; private set; }

        public int Loops { get; set; }

        public int SwitchBlocks { get; set; }

        public int FinallyBlocks { get; set; }

        public bool IsMain => Name == MAIN_FUNCTION_NAME;

        public bool IsIterator => IsMethod && !(IsContructor || IsStatic) && Name == ITERATOR_FUNCTION_NAME;

        public void PushBlock(bool asExpression)
        {
            CurrentBlock = new ParseTimeBlock(asExpression, CurrentBlock);
        }

        public void PopBlock()
        {
            CurrentBlock = CurrentBlock.Parent;
        }
    }

    #endregion

    #region ParseTimeBlock

    /// <summary>
    /// Represents a block at parse time.
    /// </summary>
    protected class ParseTimeBlock(bool asExpression, ParseTimeBlock parent)
    {
        public bool AsExpression => asExpression;

        public ParseTimeBlock Parent => parent;

        public bool CanYield => AsExpression || Parent is { CanYield: true };

        public List<ParseTimeLabel> Labels { get; } = [];

        public Dictionary<string, Label> ConvertLabels(Statement[] statements)
        {
            Dictionary<string, Label> converted = [];
            int address = 0;

            foreach (ParseTimeLabel label in Labels)
            {
                while (address < statements.Length && statements[address].Start.Offset < label.End.Offset)
                    ++address;

                converted[label.Name] = new Label(address);
            }

            return converted;
        }
    }

    #endregion

    #region ParseTimeLabel

    /// <summary>
    /// Represents a label at parse time.
    /// </summary>
    protected readonly struct ParseTimeLabel(string name, ScriptLocation start, ScriptLocation end)
    {
        public readonly string Name = name;
        public readonly ScriptLocation Start = start;
        public readonly ScriptLocation End = end;
    }

    #endregion

    #endregion
}