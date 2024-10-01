**Program:**

![Program](diagram/Program.svg)

```
Program  ::= StatementWithLabels*
```

**StatementWithLabels:**

![StatementWithLabels](diagram/StatementWithLabels.svg)

```
StatementWithLabels
         ::= Label* Statement
```

referenced by:

* Block
* Program
* SwitchBlock

**Label:**

![Label](diagram/Label.svg)

```
Label    ::= IDENTIFIER ':'
```

referenced by:

* StatementWithLabels

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
           | Yield
           | Return
           | Throw
           | TryCatchFinally
           | GroupAssignment
           | Expression ';'
```

referenced by:

* DoLoop
* ForEachLoop
* ForLoop
* IfElse
* StatementWithLabels
* WhileLoop

**ImportDirective:**

![ImportDirective](diagram/ImportDirective.svg)

```
ImportDirective
         ::= 'import' QualifiedName ( 'as' IDENTIFIER )? ';'
```

referenced by:

* Statement

**QualifiedName:**

![QualifiedName](diagram/QualifiedName.svg)

```
QualifiedName
         ::= IDENTIFIER ( '::' IDENTIFIER )*
```

referenced by:

* AtomStartingWithId
* ConstructorCall
* ImportDirective

**ClassDefinition:**

![ClassDefinition](diagram/ClassDefinition.svg)

```
ClassDefinition
         ::= Attributes? MODIFIER? 'class' IDENTIFIER ( ':' IDENTIFIER )? '{' ClassMember* '}'
```

referenced by:

* Statement

**Attributes:**

![Attributes](diagram/Attributes.svg)

```
Attributes
         ::= '[' Attribute ( ',' Attribute )* ']'
```

referenced by:

* ClassDefinition
* ExternalFunctionDecl
* FunctionDecl
* MemberPrefix
* Parameter

**Attribute:**

![Attribute](diagram/Attribute.svg)

```
Attribute
         ::= IDENTIFIER ( '(' ( Expression | ( Expression ',' )? PropertyInitializerList )? ')' )?
```

referenced by:

* Attributes

**ClassMember:**

![ClassMember](diagram/ClassMember.svg)

```
ClassMember
         ::= MemberPrefix? MemberSpec
```

referenced by:

* ClassDefinition

**MemberPrefix:**

![MemberPrefix](diagram/MemberPrefix.svg)

```
MemberPrefix
         ::= SCOPE ( MODIFIER Attributes? | Attributes MODIFIER? )?
           | MODIFIER ( SCOPE Attributes? | Attributes SCOPE? )?
           | Attributes ( SCOPE MODIFIER? | MODIFIER SCOPE? )?
```

referenced by:

* ClassMember

**MemberSpec:**

![MemberSpec](diagram/MemberSpec.svg)

```
MemberSpec
         ::= ConstructorSpec
           | FieldSpec
           | PropertySpec
           | MethodSpec
           | OperatorSpec
           | EventSpec
```

referenced by:

* ClassMember

**ConstructorSpec:**

![ConstructorSpec](diagram/ConstructorSpec.svg)

```
ConstructorSpec
         ::= 'constructor' ParameterList Block
```

referenced by:

* MemberSpec

**ParameterList:**

![ParameterList](diagram/ParameterList.svg)

```
ParameterList
         ::= '(' ( Parameter ( ',' Parameter )* )? ')'
```

referenced by:

* ConstructorSpec
* EventSpec
* ExternalFunctionDecl
* FunctionDecl
* InlineFunction
* MethodSpec
* OperatorSpec

**Parameter:**

![Parameter](diagram/Parameter.svg)

```
Parameter
         ::= ( Attributes ( '&' | '..' )? | ( '&' | '..' ) Attributes? ) IDENTIFIER '!'?
           | Attributes? IDENTIFIER '!'? ( '=' Literal )?
```

referenced by:

* Lambda
* ParameterList

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

referenced by:

* Atom
* Parameter

**FieldSpec:**

![FieldSpec](diagram/FieldSpec.svg)

```
FieldSpec
         ::= IDENTIFIER ( '=' Expression )? ';'
```

referenced by:

* MemberSpec

**PropertySpec:**

![PropertySpec](diagram/PropertySpec.svg)

```
PropertySpec
         ::= 'property' ( IDENTIFIER | '[]' ) ( ( '=>' Expression )? ';' | '{' SCOPE? ( 'read' ( MethodBody SCOPE? 'write' )? | 'write' ( MethodBody SCOPE? 'read' )? ) MethodBody '}' )
```

referenced by:

* MemberSpec

**MethodBody:**

![MethodBody](diagram/MethodBody.svg)

```
MethodBody
         ::= ( '=>' Expression )? ';'
           | Block
```

referenced by:

* MethodSpec
* OperatorSpec
* PropertySpec

**MethodSpec:**

![MethodSpec](diagram/MethodSpec.svg)

```
MethodSpec
         ::= 'function' IDENTIFIER ParameterList MethodBody
```

referenced by:

* MemberSpec

**OperatorSpec:**

![OperatorSpec](diagram/OperatorSpec.svg)

```
OperatorSpec
         ::= 'operator' OverloadableOperator ParameterList MethodBody
```

referenced by:

* MemberSpec

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

referenced by:

* OperatorSpec

**EventSpec:**

![EventSpec](diagram/EventSpec.svg)

```
EventSpec
         ::= 'event' ParameterList ';'
```

referenced by:

* MemberSpec

**FunctionDecl:**

![FunctionDecl](diagram/FunctionDecl.svg)

```
FunctionDecl
         ::= Attributes? 'function' IDENTIFIER ParameterList FunctionBody
```

referenced by:

* Statement

**FunctionBody:**

![FunctionBody](diagram/FunctionBody.svg)

```
FunctionBody
         ::= '=>' Expression ';'
           | Block
```

referenced by:

* FunctionDecl

**ExternalFunctionDecl:**

![ExternalFunctionDecl](diagram/ExternalFunctionDecl.svg)

```
ExternalFunctionDecl
         ::= Attributes? 'extern' 'function' IDENTIFIER ParameterList ';'
```

referenced by:

* Statement

**PropertyInitializerList:**

![PropertyInitializerList](diagram/PropertyInitializerList.svg)

```
PropertyInitializerList
         ::= PropertyInitializer ( ',' PropertyInitializer )*
```

referenced by:

* Attribute
* Composite
* ConstantDecl
* ConstructorCall
* ObjectInitializer
* VariableDecl

**PropertyInitializer:**

![PropertyInitializer](diagram/PropertyInitializer.svg)

```
PropertyInitializer
         ::= IDENTIFIER '=' Expression
```

referenced by:

* PropertyInitializerList

**ConstantDecl:**

![ConstantDecl](diagram/ConstantDecl.svg)

```
ConstantDecl
         ::= 'const' PropertyInitializerList ';'
```

referenced by:

* Statement

**VariableDecl:**

![VariableDecl](diagram/VariableDecl.svg)

```
VariableDecl
         ::= 'var' PropertyInitializerList ';'
```

referenced by:

* ForLoop
* Statement

**Block:**

![Block](diagram/Block.svg)

```
Block    ::= '{' StatementWithLabels* '}'
```

referenced by:

* ConstructorSpec
* FunctionBody
* InlineFunction
* Lambda
* MatchCaseExpression
* MethodBody
* Statement
* TryCatchFinally

**IfElse:**

![IfElse](diagram/IfElse.svg)

```
IfElse   ::= 'if' '(' Expression ')' Statement ( 'else' Statement )?
```

referenced by:

* Statement

**SwitchBlock:**

![SwitchBlock](diagram/SwitchBlock.svg)

```
SwitchBlock
         ::= 'switch' '(' Expression ')' '{' ( CaseLabel ':' StatementWithLabels* )* ( 'default' ':' StatementWithLabels* )? '}'
```

referenced by:

* Statement

**CaseLabel:**

![CaseLabel](diagram/CaseLabel.svg)

```
CaseLabel
         ::= 'case' ( BOOLEAN | INTEGER | STRING )
```

referenced by:

* SwitchBlock

**ForLoop:**

![ForLoop](diagram/ForLoop.svg)

```
ForLoop  ::= 'for' '(' ( VariableDecl | ExpressionList )? ';' Expression? ';' ExpressionList? ')' Statement
```

referenced by:

* Statement

**ExpressionList:**

![ExpressionList](diagram/ExpressionList.svg)

```
ExpressionList
         ::= Expression ( ',' Expression )*
```

referenced by:

* ForLoop
* GroupAssignment

**ForEachLoop:**

![ForEachLoop](diagram/ForEachLoop.svg)

```
ForEachLoop
         ::= 'foreach' '(' IDENTIFIER ( '=>' IDENTIFIER )? 'in' Expression ')' Statement
```

referenced by:

* Statement

**WhileLoop:**

![WhileLoop](diagram/WhileLoop.svg)

```
WhileLoop
         ::= 'while' '(' Expression ')' Statement
```

referenced by:

* Statement

**DoLoop:**

![DoLoop](diagram/DoLoop.svg)

```
DoLoop   ::= 'do' Statement 'while' '(' Expression ')' ';'
```

referenced by:

* Statement

**Continue:**

![Continue](diagram/Continue.svg)

```
Continue ::= 'continue' ';'
```

referenced by:

* Statement

**Break:**

![Break](diagram/Break.svg)

```
Break    ::= 'break' ';'
```

referenced by:

* Statement

**Goto:**

![Goto](diagram/Goto.svg)

```
Goto     ::= 'goto' ( IDENTIFIER | 'case' ( BOOLEAN | INTEGER | STRING ) | 'default' ) ';'
```

referenced by:

* Statement

**Yield:**

![Yield](diagram/Yield.svg)

```
Yield    ::= 'yield' Expression ';'
```

referenced by:

* Statement

**Return:**

![Return](diagram/Return.svg)

```
Return   ::= 'return' Expression? ';'
```

referenced by:

* Statement

**Throw:**

![Throw](diagram/Throw.svg)

```
Throw    ::= 'throw' Expression ';'
```

referenced by:

* Statement

**TryCatchFinally:**

![TryCatchFinally](diagram/TryCatchFinally.svg)

```
TryCatchFinally
         ::= 'try' ( '(' Expression ')' )? Block ( 'catch' '(' IDENTIFIER ')' Block )? ( 'finally' Block )?
```

referenced by:

* Statement

**GroupAssignment:**

![GroupAssignment](diagram/GroupAssignment.svg)

```
GroupAssignment
         ::= '(' ExpressionList ')' '=' '(' ListItems ')' ';'
```

referenced by:

* Statement

**ListItems:**

![ListItems](diagram/ListItems.svg)

```
ListItems
         ::= ListItem ( ',' ListItem )*
```

referenced by:

* ArgumentList
* GroupAssignment
* ListInitializer
* SetInitializer

**ListItem:**

![ListItem](diagram/ListItem.svg)

```
ListItem ::= '..'? Expression
```

referenced by:

* ListItems

**Expression:**

![Expression](diagram/Expression.svg)

```
Expression
         ::= Assignment
```

referenced by:

* AtomStartingWithSuper
* Attribute
* ComplexInitializer
* Composite
* Conversion
* DoLoop
* ExpressionList
* FieldSpec
* ForEachLoop
* ForLoop
* FunctionBody
* IfElse
* Lambda
* ListItem
* MapItemInitializer
* MatchCaseExpression
* MethodBody
* NamedArg
* ParenthesizedExpression
* Pattern
* PropertyInitializer
* PropertySpec
* Return
* Statement
* SwitchBlock
* TernaryExpression
* Throw
* TryCatchFinally
* WhileLoop
* Yield

**Assignment:**

![Assignment](diagram/Assignment.svg)

```
Assignment
         ::= TernaryExpression ( AssignmentOperator Assignment )*
```

referenced by:

* Assignment
* Expression

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

referenced by:

* Assignment

**TernaryExpression:**

![TernaryExpression](diagram/TernaryExpression.svg)

```
TernaryExpression
         ::= Condition ( '?' Expression ':' Expression )?
```

referenced by:

* Assignment

**Condition:**

![Condition](diagram/Condition.svg)

```
Condition
         ::= Relation ( LogicalOperator Relation )*
```

referenced by:

* TernaryExpression

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

referenced by:

* Condition

**Relation:**

![Relation](diagram/Relation.svg)

```
Relation ::= Term ( RelationalOperator Term | 'is' ( TYPE_NAME | IDENTIFIER ) )?
```

referenced by:

* Condition

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

referenced by:

* Relation

**Term:**

![Term](diagram/Term.svg)

```
Term     ::= Factor ( ( '+' | '-' ) Factor )*
```

referenced by:

* Relation

**Factor:**

![Factor](diagram/Factor.svg)

```
Factor   ::= Exponentiation ( ( '*' | '/' | '%' | '<<' | '>>' ) Exponentiation )*
```

referenced by:

* Term

**Exponentiation:**

![Exponentiation](diagram/Exponentiation.svg)

```
Exponentiation
         ::= PostfixUnaryExpression ( '**' Exponentiation )*
```

referenced by:

* Exponentiation
* Factor

**PostfixUnaryExpression:**

![PostfixUnaryExpression](diagram/PostfixUnaryExpression.svg)

```
PostfixUnaryExpression
         ::= PrefixUnaryExpression ( '++' | '--' | '!' )*
```

referenced by:

* Exponentiation

**PrefixUnaryExpression:**

![PrefixUnaryExpression](diagram/PrefixUnaryExpression.svg)

```
PrefixUnaryExpression
         ::= ( '+' | '-' | '~' | '!' | '++' | '--' )* Composite
```

referenced by:

* PostfixUnaryExpression

**Composite:**

![Composite](diagram/Composite.svg)

```
Composite
         ::= Atom ( '[' ( Expression | Expression? '..' Expression? ) ']' | '.' IDENTIFIER ArgumentList? | ArgumentList | ( 'switch' '{' MatchCaseList | 'with' '{' PropertyInitializerList ) '}' )*
```

referenced by:

* PrefixUnaryExpression

**ArgumentList:**

![ArgumentList](diagram/ArgumentList.svg)

```
ArgumentList
         ::= '(' ( ListItems ( ',' NamedArgList )? | NamedArgList )? ')'
```

referenced by:

* AtomStartingWithId
* AtomStartingWithSuper
* AtomStartingWithTypeName
* Composite
* ConstructorCall

**NamedArgList:**

![NamedArgList](diagram/NamedArgList.svg)

```
NamedArgList
         ::= NamedArg ( ',' NamedArg )*
```

referenced by:

* ArgumentList

**NamedArg:**

![NamedArg](diagram/NamedArg.svg)

```
NamedArg ::= IDENTIFIER ':' Expression
```

referenced by:

* NamedArgList

**MatchCaseList:**

![MatchCaseList](diagram/MatchCaseList.svg)

```
MatchCaseList
         ::= MatchCase ( ',' MatchCase )*
```

referenced by:

* Composite

**MatchCase:**

![MatchCase](diagram/MatchCase.svg)

```
MatchCase
         ::= Pattern '=>' MatchCaseExpression
```

referenced by:

* MatchCaseList

**Pattern:**

![Pattern](diagram/Pattern.svg)

```
Pattern  ::= '_'
           | 'null'
           | ValuePattern ( '..' ValuePattern? )?
           | '..' ValuePattern
           | TYPE_NAME ObjectPattern?
           | ObjectPattern
           | IDENTIFIER ':' Expression
           | CompositePattern
```

referenced by:

* CompositePattern
* MatchCase

**ValuePattern:**

![ValuePattern](diagram/ValuePattern.svg)

```
ValuePattern
         ::= [+#x2D]? ( INTEGER | BIG_INTEGER | FLOAT | BIG_DECIMAL )
           | BOOLEAN
           | DATE
           | STRING
```

referenced by:

* ObjectPattern
* Pattern

**ObjectPattern:**

![ObjectPattern](diagram/ObjectPattern.svg)

```
ObjectPattern
         ::= '{' IDENTIFIER '=' ValuePattern ( ',' IDENTIFIER '=' ValuePattern )* '}'
```

referenced by:

* Pattern

**CompositePattern:**

![CompositePattern](diagram/CompositePattern.svg)

```
CompositePattern
         ::= Pattern ( ',' Pattern )+
```

referenced by:

* Pattern

**MatchCaseExpression:**

![MatchCaseExpression](diagram/MatchCaseExpression.svg)

```
MatchCaseExpression
         ::= Block
           | 'throw'? Expression
```

referenced by:

* MatchCase

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

referenced by:

* Composite

**AtomStartingWithSuper:**

![AtomStartingWithSuper](diagram/AtomStartingWithSuper.svg)

```
AtomStartingWithSuper
         ::= 'super' ( '::' IDENTIFIER ArgumentList? | '[' Expression ']' )
```

referenced by:

* Atom

**AtomStartingWithTypeOf:**

![AtomStartingWithTypeOf](diagram/AtomStartingWithTypeOf.svg)

```
AtomStartingWithTypeOf
         ::= 'typeof' '(' ( TYPE_NAME | IDENTIFIER ) ')'
```

referenced by:

* Atom

**AtomStartingWithTypeName:**

![AtomStartingWithTypeName](diagram/AtomStartingWithTypeName.svg)

```
AtomStartingWithTypeName
         ::= TYPE_NAME '::' IDENTIFIER ArgumentList?
```

referenced by:

* Atom

**AtomStartingWithId:**

![AtomStartingWithId](diagram/AtomStartingWithId.svg)

```
AtomStartingWithId
         ::= QualifiedName ArgumentList?
```

referenced by:

* Atom

**AtomStartingWithNew:**

![AtomStartingWithNew](diagram/AtomStartingWithNew.svg)

```
AtomStartingWithNew
         ::= ObjectInitializer
           | ConstructorCall
```

referenced by:

* Atom

**ObjectInitializer:**

![ObjectInitializer](diagram/ObjectInitializer.svg)

```
ObjectInitializer
         ::= 'new' '{' PropertyInitializerList? '}'
```

referenced by:

* AtomStartingWithNew

**ConstructorCall:**

![ConstructorCall](diagram/ConstructorCall.svg)

```
ConstructorCall
         ::= 'new' QualifiedName ( ArgumentList ( '{' PropertyInitializerList? '}' )? | '{' PropertyInitializerList? '}' )
```

referenced by:

* AtomStartingWithNew

**AtomStartingWithLParen:**

![AtomStartingWithLParen](diagram/AtomStartingWithLParen.svg)

```
AtomStartingWithLParen
         ::= Conversion
           | ComplexInitializer
           | ParenthesizedExpression
```

referenced by:

* Atom

**Conversion:**

![Conversion](diagram/Conversion.svg)

```
Conversion
         ::= '(' TYPE_NAME ')' Expression
```

referenced by:

* AtomStartingWithLParen

**ComplexInitializer:**

![ComplexInitializer](diagram/ComplexInitializer.svg)

```
ComplexInitializer
         ::= '(' Expression ',' Expression ')'
```

referenced by:

* AtomStartingWithLParen

**ParenthesizedExpression:**

![ParenthesizedExpression](diagram/ParenthesizedExpression.svg)

```
ParenthesizedExpression
         ::= '(' Expression ')'
```

referenced by:

* AtomStartingWithLParen

**AtomStartingWithLBrace:**

![AtomStartingWithLBrace](diagram/AtomStartingWithLBrace.svg)

```
AtomStartingWithLBrace
         ::= SetInitializer
           | MapInitializer
```

referenced by:

* Atom

**SetInitializer:**

![SetInitializer](diagram/SetInitializer.svg)

```
SetInitializer
         ::= '{' ListItems? '}'
```

referenced by:

* AtomStartingWithLBrace

**MapInitializer:**

![MapInitializer](diagram/MapInitializer.svg)

```
MapInitializer
         ::= '{' ( MapItemInitializerList | '=>' ) '}'
```

referenced by:

* AtomStartingWithLBrace

**MapItemInitializerList:**

![MapItemInitializerList](diagram/MapItemInitializerList.svg)

```
MapItemInitializerList
         ::= MapItemInitializer ( ',' MapItemInitializer )*
```

referenced by:

* MapInitializer

**MapItemInitializer:**

![MapItemInitializer](diagram/MapItemInitializer.svg)

```
MapItemInitializer
         ::= Expression '=>' Expression
```

referenced by:

* MapItemInitializerList

**ListInitializer:**

![ListInitializer](diagram/ListInitializer.svg)

```
ListInitializer
         ::= '[' ListItems? ']'
```

referenced by:

* Atom

**Lambda:**

![Lambda](diagram/Lambda.svg)

```
Lambda   ::= '|' ( Parameter ( ',' Parameter )* )? '|' '=>' ( Expression ';' | Block )
```

referenced by:

* Atom

**InlineFunction:**

![InlineFunction](diagram/InlineFunction.svg)

```
InlineFunction
         ::= 'function' ParameterList Block
```

referenced by:

* Atom

**LETTER:**

![LETTER](diagram/LETTER.svg)

```
LETTER   ::= 'A' - 'Z'
           | 'a' - 'z'
```

referenced by:

* LETTER_EXTENDED

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

referenced by:

* SPECIAL_IDENTIFIER
* STANDARD_IDENTIFIER

**DIGIT:**

![DIGIT](diagram/DIGIT.svg)

```
DIGIT    ::= '0' - '9'
```

referenced by:

* DECIMAL_INTEGER
* HEXDIGIT
* SPECIAL_IDENTIFIER
* STANDARD_IDENTIFIER

**HEXDIGIT:**

![HEXDIGIT](diagram/HEXDIGIT.svg)

```
HEXDIGIT ::= DIGIT
           | 'A' - 'F'
           | 'a' - 'f'
```

referenced by:

* ESCAPE_SEQ
* HEX_INTEGER

**IDENTIFIER:**

![IDENTIFIER](diagram/IDENTIFIER.svg)

```
IDENTIFIER
         ::= STANDARD_IDENTIFIER
           | SPECIAL_IDENTIFIER
```

referenced by:

* AtomStartingWithSuper
* AtomStartingWithTypeName
* AtomStartingWithTypeOf
* Attribute
* ClassDefinition
* Composite
* ExternalFunctionDecl
* FieldSpec
* ForEachLoop
* FunctionDecl
* Goto
* ImportDirective
* Label
* MethodSpec
* NamedArg
* ObjectPattern
* Parameter
* Pattern
* PropertyInitializer
* PropertySpec
* QualifiedName
* Relation
* TryCatchFinally

**STANDARD_IDENTIFIER:**

![STANDARD_IDENTIFIER](diagram/STANDARD_IDENTIFIER.svg)

```
STANDARD_IDENTIFIER
         ::= LETTER_EXTENDED ( LETTER_EXTENDED | DIGIT )*
```

referenced by:

* IDENTIFIER

**SPECIAL_IDENTIFIER:**

![SPECIAL_IDENTIFIER](diagram/SPECIAL_IDENTIFIER.svg)

```
SPECIAL_IDENTIFIER
         ::= '$' ( LETTER_EXTENDED | DIGIT | ESCAPE_SEQ )+
```

referenced by:

* IDENTIFIER

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

referenced by:

* DOUBLE_QUOTED
* SINGLE_QUOTED
* SPECIAL_IDENTIFIER

**BOOLEAN:**

![BOOLEAN](diagram/BOOLEAN.svg)

```
BOOLEAN  ::= 'true'
           | 'false'
```

referenced by:

* CaseLabel
* Goto
* Literal
* ValuePattern

**INTEGER:**

![INTEGER](diagram/INTEGER.svg)

```
INTEGER  ::= DECIMAL_INTEGER
           | HEX_INTEGER
```

referenced by:

* BIG_INTEGER
* CaseLabel
* Goto
* Literal
* ValuePattern

**DECIMAL_INTEGER:**

![DECIMAL_INTEGER](diagram/DECIMAL_INTEGER.svg)

```
DECIMAL_INTEGER
         ::= ( DIGIT ( '_' DIGIT )* )+
```

referenced by:

* INTEGER
* REAL

**HEX_INTEGER:**

![HEX_INTEGER](diagram/HEX_INTEGER.svg)

```
HEX_INTEGER
         ::= '0' [Xx] HEXDIGIT+
```

referenced by:

* INTEGER

**BIG_INTEGER:**

![BIG_INTEGER](diagram/BIG_INTEGER.svg)

```
BIG_INTEGER
         ::= INTEGER [Ll]
```

referenced by:

* Literal
* ValuePattern

**REAL:**

![REAL](diagram/REAL.svg)

```
REAL     ::= ( DECIMAL_INTEGER? '.' )? DECIMAL_INTEGER ( ( 'e' | 'E' ) ( '+' | '-' )? DECIMAL_INTEGER )?
```

referenced by:

* BIG_DECIMAL
* FLOAT

**FLOAT:**

![FLOAT](diagram/FLOAT.svg)

```
FLOAT    ::= REAL [Ff]?
```

referenced by:

* Literal
* ValuePattern

**BIG_DECIMAL:**

![BIG_DECIMAL](diagram/BIG_DECIMAL.svg)

```
BIG_DECIMAL
         ::= REAL [Dd]
```

referenced by:

* Literal
* ValuePattern

**DATE:**

![DATE](diagram/DATE.svg)

```
DATE     ::= '`' [^`]* '`'
```

referenced by:

* Literal
* ValuePattern

**STRING:**

![STRING](diagram/STRING.svg)

```
STRING   ::= ( '$' '@'? )? ( SINGLE_QUOTED | DOUBLE_QUOTED )
```

referenced by:

* CaseLabel
* Goto
* Literal
* ValuePattern

**SINGLE_QUOTED:**

![SINGLE_QUOTED](diagram/SINGLE_QUOTED.svg)

```
SINGLE_QUOTED
         ::= "'" ( [^'] | ESCAPE_SEQ )* "'"
```

referenced by:

* STRING

**DOUBLE_QUOTED:**

![DOUBLE_QUOTED](diagram/DOUBLE_QUOTED.svg)

```
DOUBLE_QUOTED
         ::= '"' ( [^"] | ESCAPE_SEQ )* '"'
```

referenced by:

* STRING

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

referenced by:

* AtomStartingWithTypeName
* AtomStartingWithTypeOf
* Conversion
* Pattern
* Relation

**MODIFIER:**

![MODIFIER](diagram/MODIFIER.svg)

```
MODIFIER ::= 'final'
           | 'static'
           | 'abstract'
```

referenced by:

* ClassDefinition
* MemberPrefix

## 
![SCOPE](diagram/SCOPE.svg) <sup>generated by [RR - Railroad Diagram Generator][RR]</sup>

[RR]: https://www.bottlecaps.de/rr/ui