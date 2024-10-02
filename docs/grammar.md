# AddyScript grammar

The following railroad diagrams summarize the syntax of the AddyScript language. They were generated with the help of a tool called [_Railroad Diagram Generator_](https://rr.red-dove.com/ui). Thanks to the authors!

### Axiom

**Program:**

![Program](diagram/Program.svg)

```
Program  ::= StatementWithLabels*
```

### Non-terminal symbols

**StatementWithLabels:**

![StatementWithLabels](diagram/StatementWithLabels.svg)

```
StatementWithLabels ::= Label* Statement
```

**Label:**

![Label](diagram/Label.svg)

```
Label ::= IDENTIFIER ':'
```

**Statement:**

![Statement](diagram/Statement.svg)

```
Statement ::= ImportDirective
            | ClassDefinition
            | FunctionDecl
            | ExternalFunctionDecl
            | ConstantDecl
            | VariableDecl
            | Block
            | IfElse
            | SwitchBlock
            | ForLoop
            | ForEachLoop
            | WhileLoop
            | DoLoop
            | Continue
            | Break
            | Goto
            | Yield
            | Return
            | Throw
            | TryCatchFinally
            | GroupAssignment
            | Expression ';'
```

**ImportDirective:**

![ImportDirective](diagram/ImportDirective.svg)

```
ImportDirective ::= 'import' QualifiedName ( 'as' IDENTIFIER )? ';'
```

**QualifiedName:**

![QualifiedName](diagram/QualifiedName.svg)

```
QualifiedName ::= IDENTIFIER ( '::' IDENTIFIER )*
```

**ClassDefinition:**

![ClassDefinition](diagram/ClassDefinition.svg)

```
ClassDefinition ::= Attributes? MODIFIER? 'class' IDENTIFIER ( ':' IDENTIFIER )? '{' ClassMember* '}'
```

**Attributes:**

![Attributes](diagram/Attributes.svg)

```
Attributes ::= '[' Attribute ( ',' Attribute )* ']'
```

**Attribute:**

![Attribute](diagram/Attribute.svg)

```
Attribute ::= IDENTIFIER ( '(' ( Expression | ( Expression ',' )? PropertyInitializerList )? ')' )?
```

**ClassMember:**

![ClassMember](diagram/ClassMember.svg)

```
ClassMember ::= MemberPrefix? MemberSpec
```

**MemberPrefix:**

![MemberPrefix](diagram/MemberPrefix.svg)

```
MemberPrefix ::= SCOPE ( MODIFIER Attributes? | Attributes MODIFIER? )?
               | MODIFIER ( SCOPE Attributes? | Attributes SCOPE? )?
               | Attributes ( SCOPE MODIFIER? | MODIFIER SCOPE? )?
```

**MemberSpec:**

![MemberSpec](diagram/MemberSpec.svg)

```
MemberSpec ::= ConstructorSpec
             | FieldSpec
             | PropertySpec
             | MethodSpec
             | OperatorSpec
             | EventSpec
```

**ConstructorSpec:**

![ConstructorSpec](diagram/ConstructorSpec.svg)

```
ConstructorSpec ::= 'constructor' ParameterList Block
```

**ParameterList:**

![ParameterList](diagram/ParameterList.svg)

```
ParameterList ::= '(' ( Parameter ( ',' Parameter )* )? ')'
```

**Parameter:**

![Parameter](diagram/Parameter.svg)

```
Parameter ::= ( Attributes ( '&' | '..' )? | ( '&' | '..' ) Attributes? ) IDENTIFIER '!'? | Attributes? IDENTIFIER '!'? ( '=' Literal )?
```

**Literal:**

![Literal](diagram/Literal.svg)

```
Literal  ::= 'null'
           | BOOLEAN
           | INTEGER
           | BIG_INTEGER
           | FLOAT
           | BIG_DECIMAL
           | DATE
           | STRING
```

**FieldSpec:**

![FieldSpec](diagram/FieldSpec.svg)

```
FieldSpec ::= IDENTIFIER ( '=' Expression )? ';'
```

**PropertySpec:**

![PropertySpec](diagram/PropertySpec.svg)

```
PropertySpec ::= 'property' ( IDENTIFIER | '[]' ) ( ( '=>' Expression )? ';' | '{' SCOPE? ( 'read' ( MethodBody SCOPE? 'write' )? | 'write' ( MethodBody SCOPE? 'read' )? ) MethodBody '}' )
```
<sub>**Remarks**: if the first accessor defined has an empty body, it should be the same for the other accessor if it's defined. Both accessors cannot have custom a scope. An accessor scope shoulb always be more restrictive than the scope of the property itself</sub>

**MethodBody:**

![MethodBody](diagram/MethodBody.svg)

```
MethodBody ::= ( '=>' Expression )? ';' | Block
```

**MethodSpec:**

![MethodSpec](diagram/MethodSpec.svg)

```
MethodSpec ::= 'function' IDENTIFIER ParameterList MethodBody
```

**OperatorSpec:**

![OperatorSpec](diagram/OperatorSpec.svg)

```
OperatorSpec ::= 'operator' OverloadableOperator ParameterList MethodBody
```

**OverloadableOperator:**

![OverloadableOperator](diagram/OverloadableOperator.svg)

```
OverloadableOperator ::= '+'
                       | '-'
                       | '++'
                       | '--'
                       | '~'
                       | '*'
                       | '/'
                       | '%'
                       | '**'
                       | '&'
                       | '|'
                       | '^'
                       | '<<'
                       | '>>'
                       | '=='
                       | '!='
                       | '<'
                       | '>'
                       | '<='
                       | '>='
                       | 'startswith'
                       | 'endswith'
                       | 'contains'
                       | 'matches'
```

**EventSpec:**

![EventSpec](diagram/EventSpec.svg)

```
EventSpec ::= 'event' ParameterList ';'
```

**FunctionDecl:**

![FunctionDecl](diagram/FunctionDecl.svg)

```
FunctionDecl ::= Attributes? 'function' IDENTIFIER ParameterList FunctionBody
```

**FunctionBody:**

![FunctionBody](diagram/FunctionBody.svg)

```
FunctionBody ::= '=>' Expression ';' | Block
```

**ExternalFunctionDecl:**

![ExternalFunctionDecl](diagram/ExternalFunctionDecl.svg)

```
ExternalFunctionDecl ::= Attributes? 'extern' 'function' IDENTIFIER ParameterList ';'
```

**ConstantDecl:**

![ConstantDecl](diagram/ConstantDecl.svg)

```
ConstantDecl ::= 'const' PropertyInitializerList ';'
```

**VariableDecl:**

![VariableDecl](diagram/VariableDecl.svg)

```
VariableDecl ::= 'var' PropertyInitializerList ';'
```

**PropertyInitializerList:**

![PropertyInitializerList](diagram/PropertyInitializerList.svg)

```
PropertyInitializerList ::= PropertyInitializer ( ',' PropertyInitializer )*
```

**PropertyInitializer:**

![PropertyInitializer](diagram/PropertyInitializer.svg)

```
PropertyInitializer ::= IDENTIFIER '=' Expression
```

**Block:**

![Block](diagram/Block.svg)

```
Block ::= '{' StatementWithLabels* '}'
```

**IfElse:**

![IfElse](diagram/IfElse.svg)

```
IfElse ::= 'if' '(' Expression ')' Statement ( 'else' Statement )?
```

**SwitchBlock:**

![SwitchBlock](diagram/SwitchBlock.svg)

```
SwitchBlock ::= 'switch' '(' Expression ')' '{' ( CaseLabel ':' StatementWithLabels* )* ( 'default' ':' StatementWithLabels* )? '}'
```

**CaseLabel:**

![CaseLabel](diagram/CaseLabel.svg)

```
CaseLabel ::= 'case' ( BOOLEAN | INTEGER | STRING )
```

**ForLoop:**

![ForLoop](diagram/ForLoop.svg)

```
ForLoop ::= 'for' '(' ( VariableDecl | ExpressionList )? ';' Expression? ';' ExpressionList? ')' Statement
```

**ExpressionList:**

![ExpressionList](diagram/ExpressionList.svg)

```
ExpressionList ::= Expression ( ',' Expression )*
```

**ForEachLoop:**

![ForEachLoop](diagram/ForEachLoop.svg)

```
ForEachLoop ::= 'foreach' '(' IDENTIFIER ( '=>' IDENTIFIER )? 'in' Expression ')' Statement
```

**WhileLoop:**

![WhileLoop](diagram/WhileLoop.svg)

```
WhileLoop ::= 'while' '(' Expression ')' Statement
```

**DoLoop:**

![DoLoop](diagram/DoLoop.svg)

```
DoLoop ::= 'do' Statement 'while' '(' Expression ')' ';'
```

**Continue:**

![Continue](diagram/Continue.svg)

```
Continue ::= 'continue' ';'
```

**Break:**

![Break](diagram/Break.svg)

```
Break ::= 'break' ';'
```

**Goto:**

![Goto](diagram/Goto.svg)

```
Goto ::= 'goto' ( IDENTIFIER | 'case' ( BOOLEAN | INTEGER | STRING ) | 'default' ) ';'
```

**Yield:**

![Yield](diagram/Yield.svg)

```
Yield ::= 'yield' Expression ';'
```

**Return:**

![Return](diagram/Return.svg)

```
Return ::= 'return' Expression? ';'
```

**Throw:**

![Throw](diagram/Throw.svg)

```
Throw ::= 'throw' Expression ';'
```

**TryCatchFinally:**

![TryCatchFinally](diagram/TryCatchFinally.svg)

```
TryCatchFinally ::= 'try' ( '(' Expression ')' )? Block ( 'catch' '(' IDENTIFIER ')' Block )? ( 'finally' Block )?
```

**GroupAssignment:**

![GroupAssignment](diagram/GroupAssignment.svg)

```
GroupAssignment ::= '(' ExpressionList ')' '=' '(' ListItems ')' ';'
```

**ListItems:**

![ListItems](diagram/ListItems.svg)

```
ListItems
         ::= ListItem ( ',' ListItem )*
```

**ListItem:**

![ListItem](diagram/ListItem.svg)

```
ListItem ::= '..'? Expression
```

**Expression:**

![Expression](diagram/Expression.svg)

```
Expression ::= Assignment
```

**Assignment:**

![Assignment](diagram/Assignment.svg)

```
Assignment ::= TernaryExpression ( AssignmentOperator Assignment )*
```

**AssignmentOperator:**

![AssignmentOperator](diagram/AssignmentOperator.svg)

```
AssignmentOperator ::= '='
                     | '+='
                     | '-='
                     | '*='
                     | '/='
                     | '%='
                     | '**='
                     | '&='
                     | '|='
                     | '^='
                     | '<<='
                     | '>>='
                     | '??='
```

**TernaryExpression:**

![TernaryExpression](diagram/TernaryExpression.svg)

```
TernaryExpression ::= Condition ( '?' Expression ':' Expression )?
```

**Condition:**

![Condition](diagram/Condition.svg)

```
Condition ::= Relation ( LogicalOperator Relation )*
```

**LogicalOperator:**

![LogicalOperator](diagram/LogicalOperator.svg)

```
LogicalOperator ::= '&'
                  | '&&'
                  | '|'
                  | '||'
                  | '^'
                  | '??'
```

**Relation:**

![Relation](diagram/Relation.svg)

```
Relation ::= Term ( RelationalOperator Term | 'is' 'not'? ( TYPE_NAME | IDENTIFIER ) )?
```

**RelationalOperator:**

![RelationalOperator](diagram/RelationalOperator.svg)

```
RelationalOperator ::= '=='
                     | '!='
                     | '<'
                     | '>'
                     | '<='
                     | '>='
                     | '==='
                     | '!=='
                     | 'startswith'
                     | 'endswith'
                     | 'contains'
                     | 'matches'
```

**Term:**

![Term](diagram/Term.svg)

```
Term ::= Factor ( ( '+' | '-' ) Factor )*
```

**Factor:**

![Factor](diagram/Factor.svg)

```
Factor ::= Exponentiation ( ( '*' | '/' | '%' | '<<' | '>>' ) Exponentiation )*
```

**Exponentiation:**

![Exponentiation](diagram/Exponentiation.svg)

```
Exponentiation ::= PostfixUnaryExpression ( '**' Exponentiation )*
```

**PostfixUnaryExpression:**

![PostfixUnaryExpression](diagram/PostfixUnaryExpression.svg)

```
PostfixUnaryExpression ::= PrefixUnaryExpression ( '++' | '--' | '!' )*
```

**PrefixUnaryExpression:**

![PrefixUnaryExpression](diagram/PrefixUnaryExpression.svg)

```
PrefixUnaryExpression ::= ( '+' | '-' | '~' | '!' | '++' | '--' )* Composite
```

**Composite:**

![Composite](diagram/Composite.svg)

```
Composite ::= Atom ( '[' ( Expression | Expression? '..' Expression? ) ']' | '.' IDENTIFIER ArgumentList? | ArgumentList | ( 'switch' '{' MatchCaseList | 'with' '{' PropertyInitializerList ) '}' )*
```

**ArgumentList:**

![ArgumentList](diagram/ArgumentList.svg)

```
ArgumentList ::= '(' ( ListItems ( ',' NamedArgList )? | NamedArgList )? ')'
```

**NamedArgList:**

![NamedArgList](diagram/NamedArgList.svg)

```
NamedArgList ::= NamedArg ( ',' NamedArg )*
```

**NamedArg:**

![NamedArg](diagram/NamedArg.svg)

```
NamedArg ::= IDENTIFIER ':' Expression
```

**MatchCaseList:**

![MatchCaseList](diagram/MatchCaseList.svg)

```
MatchCaseList ::= MatchCase ( ',' MatchCase )*
```

**MatchCase:**

![MatchCase](diagram/MatchCase.svg)

```
MatchCase ::= Pattern '=>' MatchCaseExpression
```

**Pattern:**

![Pattern](diagram/Pattern.svg)

```
Pattern ::= '_'
          | 'null'
          | ValuePattern ( '..' ValuePattern? )?
          | '..' ValuePattern
          | TYPE_NAME ObjectPattern?
          | ObjectPattern
          | IDENTIFIER ':' Expression
          | CompositePattern
```

**ValuePattern:**

![ValuePattern](diagram/ValuePattern.svg)

```
ValuePattern ::= [+-]? ( INTEGER | BIG_INTEGER | FLOAT | BIG_DECIMAL )
               | BOOLEAN
               | DATE
               | STRING
```

**ObjectPattern:**

![ObjectPattern](diagram/ObjectPattern.svg)

```
ObjectPattern ::= '{' IDENTIFIER '=' ValuePattern ( ',' IDENTIFIER '=' ValuePattern )* '}'
```

**CompositePattern:**

![CompositePattern](diagram/CompositePattern.svg)

```
CompositePattern ::= Pattern ( ',' Pattern )+
```

**MatchCaseExpression:**

![MatchCaseExpression](diagram/MatchCaseExpression.svg)

```
MatchCaseExpression ::= Block | 'throw'? Expression
```

**Atom:**

![Atom](diagram/Atom.svg)

```
Atom ::= Literal
       | 'this'
       | AtomStartingWithSuper
       | AtomStartingWithTypeOf
       | AtomStartingWithTypeName
       | AtomStartingWithId
       | AtomStartingWithNew
       | AtomStartingWithLParen
       | AtomStartingWithLBrace
       | ListInitializer
       | Lambda
       | InlineFunction
```

**AtomStartingWithSuper:**

![AtomStartingWithSuper](diagram/AtomStartingWithSuper.svg)

```
AtomStartingWithSuper ::= 'super' ( '::' IDENTIFIER ArgumentList? | '[' Expression ']' )
```

**AtomStartingWithTypeOf:**

![AtomStartingWithTypeOf](diagram/AtomStartingWithTypeOf.svg)

```
AtomStartingWithTypeOf ::= 'typeof' '(' ( TYPE_NAME | IDENTIFIER ) ')'
```

**AtomStartingWithTypeName:**

![AtomStartingWithTypeName](diagram/AtomStartingWithTypeName.svg)

```
AtomStartingWithTypeName ::= TYPE_NAME '::' IDENTIFIER ArgumentList?
```

**AtomStartingWithId:**

![AtomStartingWithId](diagram/AtomStartingWithId.svg)

```
AtomStartingWithId ::= QualifiedName ArgumentList?
```

**AtomStartingWithNew:**

![AtomStartingWithNew](diagram/AtomStartingWithNew.svg)

```
AtomStartingWithNew ::= ObjectInitializer | ConstructorCall
```

**ObjectInitializer:**

![ObjectInitializer](diagram/ObjectInitializer.svg)

```
ObjectInitializer ::= 'new' '{' PropertyInitializerList? '}'
```

**ConstructorCall:**

![ConstructorCall](diagram/ConstructorCall.svg)

```
ConstructorCall ::= 'new' QualifiedName ( ArgumentList ( '{' PropertyInitializerList? '}' )? | '{' PropertyInitializerList? '}' )
```

**AtomStartingWithLParen:**

![AtomStartingWithLParen](diagram/AtomStartingWithLParen.svg)

```
AtomStartingWithLParen ::= Conversion | ComplexInitializer | ParenthesizedExpression
```

**Conversion:**

![Conversion](diagram/Conversion.svg)

```
Conversion ::= '(' TYPE_NAME ')' Expression
```

**ComplexInitializer:**

![ComplexInitializer](diagram/ComplexInitializer.svg)

```
ComplexInitializer ::= '(' Expression ',' Expression ')'
```

**ParenthesizedExpression:**

![ParenthesizedExpression](diagram/ParenthesizedExpression.svg)

```
ParenthesizedExpression ::= '(' Expression ')'
```

**AtomStartingWithLBrace:**

![AtomStartingWithLBrace](diagram/AtomStartingWithLBrace.svg)

```
AtomStartingWithLBrace ::= SetInitializer | MapInitializer
```

**SetInitializer:**

![SetInitializer](diagram/SetInitializer.svg)

```
SetInitializer ::= '{' ListItems? '}'
```

**MapInitializer:**

![MapInitializer](diagram/MapInitializer.svg)

```
MapInitializer ::= '{' ( MapItemInitializerList | '=>' ) '}'
```

**MapItemInitializerList:**

![MapItemInitializerList](diagram/MapItemInitializerList.svg)

```
MapItemInitializerList ::= MapItemInitializer ( ',' MapItemInitializer )*
```

**MapItemInitializer:**

![MapItemInitializer](diagram/MapItemInitializer.svg)

```
MapItemInitializer ::= Expression '=>' Expression
```

**ListInitializer:**

![ListInitializer](diagram/ListInitializer.svg)

```
ListInitializer ::= '[' ListItems? ']'
```

**Lambda:**

![Lambda](diagram/Lambda.svg)

```
Lambda ::= '|' ( Parameter ( ',' Parameter )* )? '|' '=>' ( Expression ';' | Block )
```

**InlineFunction:**

![InlineFunction](diagram/InlineFunction.svg)

```
InlineFunction ::= 'function' ParameterList Block
```

### Terminal symbols

**LETTER:**

![LETTER](diagram/LETTER.svg)

```
LETTER ::= 'A' - 'Z' | 'a' - 'z'
```

**LETTER_EXTENDED:**

![LETTER_EXTENDED](diagram/LETTER_EXTENDED.svg)

```
LETTER_EXTENDED ::= LETTER
                  | '_'
                  | '\xc0' - '\xd6'
                  | '\xd8' - '\xf6'
                  | '\xf8' - '\xff'
```

**DIGIT:**

![DIGIT](diagram/DIGIT.svg)

```
DIGIT ::= '0' - '9'
```

**HEXDIGIT:**

![HEXDIGIT](diagram/HEXDIGIT.svg)

```
HEXDIGIT ::= DIGIT
           | 'A' - 'F'
           | 'a' - 'f'
```

**IDENTIFIER:**

![IDENTIFIER](diagram/IDENTIFIER.svg)

```
IDENTIFIER ::= STANDARD_IDENTIFIER | SPECIAL_IDENTIFIER
```

**STANDARD_IDENTIFIER:**

![STANDARD_IDENTIFIER](diagram/STANDARD_IDENTIFIER.svg)

```
STANDARD_IDENTIFIER ::= LETTER_EXTENDED ( LETTER_EXTENDED | DIGIT )*
```

**SPECIAL_IDENTIFIER:**

![SPECIAL_IDENTIFIER](diagram/SPECIAL_IDENTIFIER.svg)

```
SPECIAL_IDENTIFIER ::= '$' ( LETTER_EXTENDED | DIGIT | ESCAPE_SEQ )+
```

**ESCAPE_SEQ:**

![ESCAPE_SEQ](diagram/ESCAPE_SEQ.svg)

```
ESCAPE_SEQ ::= '\a'
             | '\b'
             | '\f'
             | '\n'
             | '\r'
             | '\t'
             | '\v'
             | ( '\x' | '\u' HEXDIGIT HEXDIGIT ) HEXDIGIT HEXDIGIT
```

**BOOLEAN:**

![BOOLEAN](diagram/BOOLEAN.svg)

```
BOOLEAN ::= 'true' | 'false'
```

**INTEGER:**

![INTEGER](diagram/INTEGER.svg)

```
INTEGER ::= DECIMAL_INTEGER | HEX_INTEGER
```

**DECIMAL_INTEGER:**

![DECIMAL_INTEGER](diagram/DECIMAL_INTEGER.svg)

```
DECIMAL_INTEGER ::= ( DIGIT ( '_' DIGIT )* )+
```

**HEX_INTEGER:**

![HEX_INTEGER](diagram/HEX_INTEGER.svg)

```
HEX_INTEGER ::= '0' [Xx] HEXDIGIT+
```

**BIG_INTEGER:**

![BIG_INTEGER](diagram/BIG_INTEGER.svg)

```
BIG_INTEGER ::= INTEGER [Ll]
```

**REAL:**

![REAL](diagram/REAL.svg)

```
REAL ::= ( DECIMAL_INTEGER? '.' )? DECIMAL_INTEGER ( ( 'e' | 'E' ) ( '+' | '-' )? DECIMAL_INTEGER )?
```

**FLOAT:**

![FLOAT](diagram/FLOAT.svg)

```
FLOAT ::= REAL [Ff]?
```

**BIG_DECIMAL:**

![BIG_DECIMAL](diagram/BIG_DECIMAL.svg)

```
BIG_DECIMAL ::= REAL [Dd]
```

**DATE:**

![DATE](diagram/DATE.svg)

```
DATE ::= '`' [^`]* '`'
```

**STRING:**

![STRING](diagram/STRING.svg)

```
STRING ::= ( '$' '@'? )? ( SINGLE_QUOTED | DOUBLE_QUOTED )
```

**SINGLE_QUOTED:**

![SINGLE_QUOTED](diagram/SINGLE_QUOTED.svg)

```
SINGLE_QUOTED ::= "'" ( [^'] | ESCAPE_SEQ )* "'"
```

**DOUBLE_QUOTED:**

![DOUBLE_QUOTED](diagram/DOUBLE_QUOTED.svg)

```
DOUBLE_QUOTED ::= '"' ( [^"] | ESCAPE_SEQ )* '"'
```

**TYPE_NAME:**

![TYPE_NAME](diagram/TYPE_NAME.svg)

```
TYPE_NAME ::= 'void'
            | 'bool'
            | 'int'
            | 'long'
            | 'rational'
            | 'float'
            | 'decimal'
            | 'complex'
            | 'date'
            | 'string'
            | 'list'
            | 'map'
            | 'set'
            | 'queue'
            | 'stack'
            | 'object'
            | 'resource'
            | 'closure'
```

**MODIFIER:**

![MODIFIER](diagram/MODIFIER.svg)

```
MODIFIER ::= 'final'
           | 'static'
           | 'abstract'
```

**SCOPE**

![SCOPE](diagram/SCOPE.svg)

```
SCOPE ::= 'private'
        | 'protected'
        | 'public'
```

[Home](README.md) | [Previous](exceptions.md) | [Next](extapi.md)