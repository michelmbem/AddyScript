using System;
using System.Collections.Generic;
using System.Diagnostics;

using AddyScript.Ast;
using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime;


namespace AddyScript.Parsers
{
    /// <summary>
    /// The base class of all AddyScript's parsers.<br/>
    /// A collection of utility methods for derived classes.
    /// </summary>
    public class BaseParser
    {
        #region Fields

        private const int MAX_BUFFER_SIZE = 100;
        private const string MAIN_FUNCTION_NAME = "__main__";

        private readonly Lexer lexer;
        private readonly List<Token> tokens = new List<Token>();
        private int tokenIndex;

        protected Token token;

        #endregion

        #region Constructor and properties

        /// <summary>
        /// Initializes a new instance of BaseParser
        /// </summary>
        /// <param name="lexer">The bound lexer</param>
        public BaseParser(Lexer lexer)
        {
            // Ensures that the lexer is not null
            if (lexer == null) throw new ArgumentNullException("lexer");

            // Initializes the lexer and peeks the first token from it.
            this.lexer = lexer;
            token = Ll(1);

            // Stores the default '__main' function on top of functions hierarchy
            PushFunction(MAIN_FUNCTION_NAME, false, false, false);
        }

        /// <summary>
        /// Gets the name of the source file being parsed.
        /// </summary>
        public string FileName
        {
            get { return lexer.FileName; }
        }

        /// <summary>
        /// Gets the currently parsed class
        /// </summary>
        protected ParseTimeClass CurrentClass { get; private set; }

        /// <summary>
        /// Gets the currently parsed function or method
        /// </summary>
        protected ParseTimeFunction CurrentFunction { get; private set; }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a token without removing it from the buffer.
        /// </summary>
        /// <param name="k">The relative index of the token to peek</param>
        /// <returns>A <see cref="Token"/></returns>
        protected Token Ll(int k)
        {
            Debug.Assert(k > 0);

            int realIndex = tokenIndex + k;

            while (tokens.Count < realIndex)
                tokens.Add(lexer.NextToken());

            return tokens[realIndex - 1];
        }

        /// <summary>
        /// Skips a number of tokens and peeks the next from the lexer.
        /// </summary>
        /// <param name="count">The number of tokens to skip</param>
        protected void Consume(int count)
        {
            Debug.Assert(count > 0 && tokenIndex + count <= tokens.Count);

            tokenIndex += count;
            token = Ll(1);

            while (tokenIndex >= MAX_BUFFER_SIZE)
            {
                tokens.RemoveRange(0, MAX_BUFFER_SIZE);
                tokenIndex -= MAX_BUFFER_SIZE;
            }
        }

        /// <summary>
        /// Skips successive comments until a <i>non-comment</i> token is encountered.
        /// </summary>
        protected void SkipComments()
        {
            while (token.IsComment)
                Consume(1);
        }

        /// <summary>
        /// Requires that the next <i>non-comment</i> token have the given ID.<br/>
        /// Throws a <see cref="ParseException"/> if another ID is encountered.<br/>
        /// Consumes the token otherwise.
        /// </summary>
        /// <param name="requiredID">The ID we want to match</param>
        /// <returns>The matching token</returns>
        protected Token Match(TokenID requiredID)
        {
            SkipComments();
            if (token.TokenID != requiredID)
                throw new ParseException(FileName, token);

            Token found = token;
            Consume(1);

            return found;
        }

        /// <summary>
        /// Requires that the next <i>non-comment</i> token have one of the given IDs.<br/>
        /// Throws a <see cref="ParseException"/> if another ID is encountered.<br/>
        /// Consumes the token otherwise.
        /// </summary>
        /// <param name="requiredIDs">The IDs we want to match</param>
        /// <returns>The matching token</returns>
        protected Token MatchAny(params TokenID[] requiredIDs)
        {
            SkipComments();

            foreach (TokenID id in requiredIDs)
            {
                if (token.TokenID != id) continue;
                Token found = token;
                Consume(1);
                return found;
            }

            throw new ParseException(FileName, token);
        }

        /// <summary>
        /// Tests if the next <i>non-comment</i> token has the given ID.<br/>
        /// </summary>
        /// <param name="requiredID">The ID we may want to match</param>
        /// <returns><b>true</b> if the token's ID matches the given ID; <b>false</b> otherwise</returns>
        protected bool TryMatch(TokenID requiredID)
        {
            SkipComments();
            return token.TokenID == requiredID;
        }

        /// <summary>
        /// Tests if the next <i>non-comment</i> token has one of the given IDs.<br/>
        /// </summary>
        /// <param name="requiredIDs">The IDs we want to match</param>
        /// <returns><b>true</b> if the token's ID matches one of the given IDs; <b>false</b> otherwise</returns>
        protected bool TryMatchAny(params TokenID[] requiredIDs)
        {
            SkipComments();

            foreach (TokenID id in requiredIDs)
                if (token.TokenID == id) return true;

            return false;
        }

        /// <summary>
        /// Lookups a specific terminal symbol ahead the token stream
        /// starting from the position that follows the current position.
        /// </summary>
        /// <param name="lookupID">The ID to find</param>
        /// <param name="k">The position of the matching token if any</param>
        /// <returns><b>true</b> if a matching token is found ahead; <b>false</b> otherwise</returns>
        protected bool LookAhead(TokenID lookupID, out int k)
        {
            k = 2;
            Token tmpTok = Ll(k);
            while (tmpTok.IsComment) tmpTok = Ll(++k);

            return tmpTok.TokenID == lookupID;
        }

        /// <summary>
        /// Executes some parsing method and verifies that the returned value is non-null.
        /// </summary>
        /// <typeparam name="T">The type of element to be recognized</typeparam>
        /// <param name="recognizer">The recognition method</param>
        /// <param name="errorMessage">
        /// The message of the exception to be thrown whenever <paramref name="recognizer"/> returns null
        /// </param>
        /// <returns>A non-null instance of the desired type</returns>
        protected T Required<T>(Recognizer<T> recognizer, string errorMessage)
            where T : ScriptElement
        {
            T element = recognizer();
            if (element == null)
                throw new ParseException(FileName, token, errorMessage);

            return element;
        }

        /// <summary>
        /// Recognizes a sequence of non-terminal symbols of the same type.
        /// </summary>
        /// <typeparam name="T">The type of non-terminal symbols to be recognized</typeparam>
        /// <param name="recognizer">The method used to recognize each symbol</param>
        /// <returns>An array of instances of the desired type</returns>
        protected T[] Asterisk<T>(Recognizer<T> recognizer)
            where T : ScriptElement
        {
            var elements = new List<T>();
            T element = recognizer();

            while (element != null)
            {
                elements.Add(element);
                element = recognizer();
            }

            return elements.ToArray();
        }

        /// <summary>
        /// Recognizes a non-empty sequence of non-terminal symbols of the same type.
        /// </summary>
        /// <typeparam name="T">The type of non-terminal symbols to be recognized</typeparam>
        /// <param name="recognizer">The method used to recognize each symbol</param>
        /// <param name="errorMessage">The message of the exception thrown if the list is empty</param>
        /// <returns>A non-empty array of instances of the desired type</returns>
        protected T[] Plus<T>(Recognizer<T> recognizer, string errorMessage)
            where T : ScriptElement
        {
            T[] elements = Asterisk(recognizer);
            if (elements.Length <= 0)
                throw new ParseException(FileName, token, errorMessage);

            return elements;
        }

        /// <summary>
        /// Recognizes a comma separated list of non-terminal symbols of the same type.
        /// </summary>
        /// <typeparam name="T">The type of non-terminal symbols to be recognized</typeparam>
        /// <param name="recognizer">The method used to recognize each symbol</param>
        /// <param name="checkUnicity">Tells if each symbol must be unique</param>
        /// <param name="errorMessage">The message of the exception thrown if a symbol is duplicated in the list</param>
        /// <returns>An array of instances of the desired type</returns>
        protected T[] List<T>(Recognizer<T> recognizer, bool checkUnicity, string errorMessage)
            where T : ScriptElement
        {
            T element = recognizer();
            if (element == null) return new T[0];

            var elements = new List<T> { element };

            while (TryMatch(TokenID.Comma))
            {
                Consume(1);

                element = Required(recognizer, Resources.InvalidListTermination);
                if (checkUnicity && elements.Contains(element))
                    throw new ScriptException(FileName, element, errorMessage);

                elements.Add(element);
            }

            return elements.ToArray();
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
            CurrentClass = CurrentClass.Next;
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
            CurrentFunction = CurrentFunction.Next;
        }

        #endregion

        #region Nested types

        /// <summary>
        /// A common prototype for non-terminal symbols recognition methods.
        /// </summary>
        /// <typeparam name="T">The returned symbol's type</typeparam>
        /// <returns>An instance of the desired type</returns>
        protected delegate T Recognizer<T>() where T : ScriptElement;

        #region ParseTimeClass

        /// <summary>
        /// Represents a class at parse time.
        /// </summary>
        protected class ParseTimeClass
        {
            public ParseTimeClass(Modifier modifier, string name, string parentName, ParseTimeClass next)
            {
                Modifier = modifier;
                Name = name;
                ParentName = parentName;
                Next = next;
            }

            public Modifier Modifier { get; private set; }
            public string Name { get; private set; }
            public string ParentName { get; private set; }
            public ParseTimeClass Next { get; private set; }
        }

        #endregion

        #region ParseTimeFunction

        /// <summary>
        /// Represents a function at parse time.
        /// </summary>
        protected class ParseTimeFunction
        {
            public ParseTimeFunction(string name, bool isMethod, bool isContructor, bool isStatic, ParseTimeFunction next)
            {
                Name = name;
                IsMethod = isMethod;
                IsContructor = isContructor;
                IsStatic = isStatic;
                Next = next;
                PushBlock();
            }

            public string Name { get; private set; }
            public bool IsMethod { get; private set; }
            public bool IsContructor { get; private set; }
            public bool IsStatic { get; private set; }
            public ParseTimeFunction Next { get; private set; }
            public ParseTimeBlock CurrentBlock { get; private set; }
            public int Loops { get; set; }
            public int SwitchBlocks { get; set; }
            public int FinallyBlocks { get; set; }

            public bool IsMain
            {
                get { return Name == MAIN_FUNCTION_NAME; }
            }

            public void PushBlock()
            {
                CurrentBlock = new ParseTimeBlock(CurrentBlock);
            }

            public void PopBlock()
            {
                CurrentBlock = CurrentBlock.Next;
            }
        }

        #endregion

        #region ParseTimeBlock

        /// <summary>
        /// Represents a block at parse time.
        /// </summary>
        protected class ParseTimeBlock
        {
            public ParseTimeBlock(ParseTimeBlock next)
            {
                Labels = new List<ParseTimeLabel>();
                Next = next;
            }

            public List<ParseTimeLabel> Labels { get; private set; }
            public ParseTimeBlock Next { get; private set; }

            public Dictionary<string, Label> ConvertLabels(AstNode[] nodes)
            {
                var converted = new Dictionary<string, Label>();
                int counter = 0;

                foreach (ParseTimeLabel label in Labels)
                {
                    while (counter < nodes.Length &&
                           nodes[counter].Start.Offset < label.End.Offset)
                        ++counter;

                    converted[label.Name] = new Label(counter);
                }

                return converted;
            }
        }

        #endregion

        #region ParseTimeLabel

        /// <summary>
        /// Represents a label at parse time.
        /// </summary>
        protected struct ParseTimeLabel
        {
            public string Name;
            public ScriptLocation Start;
            public ScriptLocation End;

            public ParseTimeLabel(string name, ScriptLocation start, ScriptLocation end)
            {
                Name = name;
                Start = start;
                End = end;
            }
        }

        #endregion

        #endregion
    }
}