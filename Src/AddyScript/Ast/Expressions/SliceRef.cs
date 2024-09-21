﻿using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to a range of items a list.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="SliceRef"/>
    /// </remarks>
    /// <param name="owner">The collection to which this item belongs</param>
    /// <param name="lowerBound">The lower bound of the items range</param>
    /// <param name="upperBound">The upper bound of the items range</param>
    public class SliceRef(Expression owner, Expression lowerBound, Expression upperBound) : Expression
    {

        /// <summary>
        /// >The collection to which this item belongs.
        /// </summary>
        public Expression Owner { get; private set; } = owner;

        /// <summary>
        /// The lower bound of the items range.
        /// </summary>
        public Expression LowerBound { get; private set; } = lowerBound;

        /// <summary>
        /// The upper bound of the items range.
        /// </summary>
        public Expression UpperBound { get; private set; } = upperBound;

        /// <summary>
        /// Determines whether to stop null reference propagation or not.
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// A factory method to quickly create instances of <see cref="SliceRef"/>
        /// where the owner is always the keyword <i>this</i>.
        /// </summary>
        /// <param name="lowerBound">The expression used to evaluate the lowerBound</param>
        /// <returns>An <see cref="SliceRef"/></returns>
        public static Expression This(Expression lowerBound, Expression upperBound)
        {
            return new SliceRef(new SelfReference(), lowerBound, upperBound);
        }

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateSliceRef(this);
        }
    }
}