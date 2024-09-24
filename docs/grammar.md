# AddyScript grammar

The following railroad diagrams summarize the syntax of the AddyScript language. They were generated with the help of a tool called [_Railroad Diagram Generator_](https://rr.red-dove.com/ui). Thanks to the authors!

Note that in the AddyScript grammar, the axiom is a symbol called _Program_ ; it represents an entire script. Here are the syntax rules:

<style>
    img {
        background-color: #FFE;
        padding: 8px;
    }
</style>

## Non-terminal symbols

**Program:**

![Program](diagram/Program.svg)

```
Program  ::= StatementSeries
```

**StatementSeries:**

![StatementSeries](diagram/StatementSeries.svg)

```
StatementSeries ::= ( Label* Statement )*
```

**Label:**

![Label](diagram/Label.svg)

```
Label ::= IDENTIFIER ':'
```

**Statement:**

![Statement](diagram/Statement.svg)

```
Statement
         ::= ImportDirective
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
           | Return
           | Throw
           | TryCatchFinally
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
Attribute ::= IDENTIFIER ( '(' ( Expression | ( Expression ',' )? PropertyInitializers )? ')' )?
```

**PropertyInitializers:**

![PropertyInitializers](diagram/PropertyInitializers.svg)

```
PropertyInitializers ::= PropertyInitializer ( ',' PropertyInitializer )*
```

**PropertyInitializer:**

![PropertyInitializer](diagram/PropertyInitializer.svg)

```
PropertyInitializer ::= IDENTIFIER '=' Expression
```

**ClassMember:**

![ClassMember](diagram/ClassMember.svg)

```
ClassMember ::= MemberPrefix? MemberSpec
```

**MemberPrefix:**

![MemberPrefix](diagram/MemberPrefix.svg)

```
MemberPrefix
         ::= SCOPE ( MODIFIER Attributes? | Attributes MODIFIER? )?
           | MODIFIER ( SCOPE Attributes? | Attributes SCOPE? )?
           | Attributes ( SCOPE MODIFIER? | MODIFIER SCOPE? )?
```

**MemberSpec:**

![MemberSpec](diagram/MemberSpec.svg)

```
MemberSpec
         ::= ConstructorSpec
           | FieldSpec
           | PropertySpec
           | MethodSpec
           | EventSpec
```

**ConstructorSpec:**

![ConstructorSpec](diagram/ConstructorSpec.svg)

```
ConstructorSpec ::= 'constructor' ParameterList Block
```

**FieldSpec:**

![FieldSpec](diagram/FieldSpec.svg)

```
FieldSpec ::= IDENTIFIER ( '=' Expression )? ';'
```

**PropertySpec:**

![PropertySpec](diagram/PropertySpec.svg)

```
PropertySpec ::= 'property' PropertyName ( ExpandedPropertySpec | AutoPropertySpec )
```

**PropertyName:**

![PropertyName](diagram/PropertyName.svg)

```
PropertyName ::= IDENTIFIER | '[]'
```
<sub>**Remark**: the property is an indexer when its name is a pair of brackets</sub>

**ExpandedPropertySpec:**

![ExpandedPropertySpec](diagram/ExpandedPropertySpec.svg)

```
ExpandedPropertySpec
         ::= '=>' Expression ';'
           | '{' ( SCOPE? ( 'read' | 'write' ) FunctionBody )+ '}'
```
<sub>**Remark**: each accessor can only be defined once</sub>

**FunctionBody:**

![FunctionBody](diagram/FunctionBody.svg)

```
FunctionBody
         ::= '=>' Expression ';'
           | Block
```

**AutoPropertySpec:**

![AutoPropertySpec](diagram/AutoPropertySpec.svg)

```
AutoPropertySpec
         ::= '{' ( SCOPE? ( 'read' | 'write' ) ';' )+ '}'
           | ';'
```
<sub>**Remark**: each accessor can only be declared once</sub>

**MethodSpec:**

![MethodSpec](diagram/MethodSpec.svg)

```
MethodSpec
         ::= AbstractMethodSpec
           | StandardMethodSpec
           | OperatorSpec
```

**AbstractMethodSpec:**

![AbstractMethodSpec](diagram/AbstractMethodSpec.svg)

```
AbstractMethodSpec ::= 'function' IDENTIFIER ParameterList ';'
```

**StandardMethodSpec:**

![StandardMethodSpec](diagram/StandardMethodSpec.svg)

```
StandardMethodSpec ::= 'function' IDENTIFIER ParameterList FunctionBody
```

**OperatorSpec:**

![OperatorSpec](diagram/OperatorSpec.svg)

```
OperatorSpec ::= 'operator' OverloadableOperator ParameterList FunctionBody
```

**ParameterList:**

![ParameterList](diagram/ParameterList.svg)

```
ParameterList ::= '(' ( Parameter ( ',' Parameter )* )? ')'
```

**Parameter:**

![Parameter](diagram/Parameter.svg)

```
Parameter ::= ParameterPrefix IDENTIFIER | IDENTIFIER ( '=' Literal )?
```

**ParameterPrefix:**

![ParameterPrefix](diagram/ParameterPrefix.svg)

```
ParameterPrefix ::= 'ref' | 'params'
```

**OverloadableOperator:**

![OverloadableOperator](diagram/OverloadableOperator.svg)

```
OverloadableOperator
         ::= '+'
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

**ExternalFunctionDecl:**

![ExternalFunctionDecl](diagram/ExternalFunctionDecl.svg)

```
ExternalFunctionDecl ::= Attributes? 'extern' 'function' IDENTIFIER ParameterList ';'
```

**ConstantDecl:**

![ConstantDecl](diagram/ConstantDecl.svg)

```
ConstantDecl ::= 'const' PropertyInitializers ';'
```

**VariableDecl:**

![VariableDecl](diagram/VariableDecl.svg)

```
VariableDecl ::= 'var' PropertyInitializers ';'
```

**Block:**

![Block](diagram/Block.svg)

```
Block ::= '{' StatementSeries '}'
```

**IfElse:**

![IfElse](diagram/IfElse.svg)

```
IfElse ::= 'if' '(' Expression ')' Statement ( 'else' Statement )?
```

**SwitchBlock:**

![SwitchBlock](diagram/SwitchBlock.svg)

```
SwitchBlock ::= 'switch' '(' Expression ')' '{' ( CaseLabel StatementSeries )+ '}'
```

**CaseLabel:**

![CaseLabel](diagram/CaseLabel.svg)

```
CaseLabel ::= ( 'case' ( BOOLEAN | INTEGER | STRING ) | 'default' ) ':'
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
Goto ::= 'goto' IDENTIFIER ';'
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
AssignmentOperator
         ::= '='
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
<sub>**Remark**: associativity to the left</sub>

**LogicalOperator:**

![LogicalOperator](diagram/LogicalOperator.svg)

```
LogicalOperator
         ::= '&'
           | '&&'
           | '|'
           | '||'
           | '^'
           | '??'
```

**Relation:**

![Relation](diagram/Relation.svg)

```
Relation ::= Term ( RelationalOperator Term | 'is' ( TYPE_NAME | IDENTIFIER ) )?
```

**RelationalOperator:**

![RelationalOperator](diagram/RelationalOperator.svg)

```
RelationalOperator
         ::= '=='
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
<sub>**Remark**: associativity to the left</sub>

**Factor:**

![Factor](diagram/Factor.svg)

```
Factor ::= Exponentiation ( ( '*' | '/' | '%' | '<<' | '>>' ) Exponentiation )*
```
<sub>**Remark**: associativity to the left</sub>

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
Composite ::= Atom ( '[' ( Expression | Expression? '..' Expression? ) ']' | '.' IDENTIFIER ArgumentList? | ArgumentList | ( 'switch' '{' MatchCases | 'with' '{' PropertyInitializers ) '}' )*
```

**ArgumentList:**

![ArgumentList](diagram/ArgumentList.svg)

```
ArgumentList ::= '(' ( ExpressionList ( ',' NamedArgList )? | NamedArgList )? ')'
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

**MatchCases:**

![MatchCases](diagram/MatchCases.svg)

```
MatchCases ::= MatchCase ( ',' MatchCase )*
```

**MatchCase:**

![MatchCase](diagram/MatchCase.svg)

```
MatchCase ::= Pattern '=>' MatchCaseExpression
```

**Pattern:**

![Pattern](diagram/Pattern.svg)

```
Pattern  ::= '_'
           | 'null'
           | TYPE_NAME ObjectPattern?
           | ObjectPattern
           | ValuePattern
           | RangePattern
           | PredicatePattern
           | CompositePattern
```

**ObjectPattern:**

![ObjectPattern](diagram/ObjectPattern.svg)

```
ObjectPattern ::= '{' IDENTIFIER '=' ValuePattern ( ',' IDENTIFIER '=' ValuePattern )* '}'
```

**ValuePattern:**

![ValuePattern](diagram/ValuePattern.svg)

```
ValuePattern ::= ( '+' | '-' )? ( BOOLEAN | INTEGER | BIG_INTEGER | FLOAT | BIG_DECIMAL | DATE | STRING )
```

**RangePattern:**

![RangePattern](diagram/RangePattern.svg)

```
RangePattern
         ::= ValuePattern '..' ValuePattern?
           | '..' ValuePattern
```

**PredicatePattern:**

![PredicatePattern](diagram/PredicatePattern.svg)

```
PredicatePattern ::= IDENTIFIER ':' Expression
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
Atom     ::= Literal
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
ObjectInitializer ::= 'new' '{' PropertyInitializers? '}'
```

**ConstructorCall:**

![ConstructorCall](diagram/ConstructorCall.svg)

```
ConstructorCall ::= 'new' QualifiedName ArgumentList? ( '{' PropertyInitializers? '}' )?
```

**AtomStartingWithLParen:**

![AtomStartingWithLParen](diagram/AtomStartingWithLParen.svg)

```
AtomStartingWithLParen
         ::= Conversion
           | ComplexInitializer
           | ParenthesizedExpression
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
SetInitializer ::= '{' ExpressionList? '}'
```

**MapInitializer:**

![MapInitializer](diagram/MapInitializer.svg)

```
MapInitializer ::= '{' ( MapItemInitializers | '=>' ) '}'
```

**MapItemInitializers:**

![MapItemInitializers](diagram/MapItemInitializers.svg)

```
MapItemInitializers ::= MapItemInitializer ( ',' MapItemInitializer )*
```

**MapItemInitializer:**

![MapItemInitializer](diagram/MapItemInitializer.svg)

```
MapItemInitializer ::= Expression '=>' Expression
```

**ListInitializer:**

![ListInitializer](diagram/ListInitializer.svg)

```
ListInitializer ::= '[' ExpressionList? ']'
```

**Lambda:**

![Lambda](diagram/Lambda.svg)

```
Lambda ::= '|' ( Parameter ( ',' Parameter )* )? '|' '=>' FunctionBody
```

**InlineFunction:**

![InlineFunction](diagram/InlineFunction.svg)

```
InlineFunction ::= 'function' ParameterList Block
```

## Terminal symbols

**LETTER:**

![LETTER](diagram/LETTER.svg)

```
LETTER   ::= 'A' - 'Z' | 'a' - 'z'
```

**LETTER_EXTENDED:**

![LETTER_EXTENDED](diagram/LETTER_EXTENDED.svg)

```
LETTER_EXTENDED
         ::= LETTER
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
HEXDIGIT ::= DIGIT | 'A' - 'F' | 'a' - 'f'
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
ESCAPE_SEQ
         ::= '\a'
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
BOOLEAN  ::= 'true' | 'false'
```

**INTEGER:**

![INTEGER](diagram/INTEGER.svg)

```
INTEGER  ::= DECIMAL_INTEGER | HEX_INTEGER
```

**DECIMAL_INTEGER:**

![DECIMAL_INTEGER](diagram/DECIMAL_INTEGER.svg)

```
DECIMAL_INTEGER ::= ( DIGIT ( '_' DIGIT )* )+
```

**HEX_INTEGER:**

![HEX_INTEGER](diagram/HEX_INTEGER.svg)

```
HEX_INTEGER ::= ( '0x' | '0X' ) HEXDIGIT+
```

**BIG_INTEGER:**

![BIG_INTEGER](diagram/BIG_INTEGER.svg)

```
BIG_INTEGER ::= INTEGER ( 'l' | 'L' )
```

**FLOAT:**

![FLOAT](diagram/FLOAT.svg)

```
FLOAT ::= DECIMAL_INTEGER ( '.' DECIMAL_INTEGER )? ( ( 'e' | 'E' ) ( '+' | '-' )? DECIMAL_INTEGER )? ( 'f' | 'F' )?
```

**BIG_DECIMAL:**

![BIG_DECIMAL](diagram/BIG_DECIMAL.svg)

```
BIG_DECIMAL ::= DECIMAL_INTEGER ( '.' DECIMAL_INTEGER )? ( ( 'e' | 'E' ) ( '+' | '-' )? DECIMAL_INTEGER )? ( 'd' | 'D' )
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
TYPE_NAME
         ::= 'void'
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

**SCOPE:**

![SCOPE](diagram/SCOPE.svg)

```
SCOPE    ::= 'private'
           | 'protected'
           | 'public'
```

[Home](README.md) | [Previous](exceptions.md) | [Next](extapi.md)