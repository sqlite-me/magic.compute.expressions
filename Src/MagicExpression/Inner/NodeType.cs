using System;
using System.Collections.Generic;
using System.Text;

namespace MagicExpression.Inner
{
    internal enum NodeType
    {
        Const,
        Param,
        Variable,
        /// <summary>
        /// !
        /// </summary>
        Operator_Single,
        /// <summary>
        /// + - * / % == !== > &lt; >= &lt;= | &amp; &lt;&lt; >> ^ ?? 
        /// </summary>
        Operator_Double,
        /// <summary>
        /// ? :
        /// </summary>
        Operator_Ternary,
        /// <summary>
        /// .[ProName]
        /// </summary>
        Operator_Access,
        /// <summary>
        /// is (is not) as
        /// </summary>
        Operator_Type,
        /// <summary>
        /// ()
        /// </summary>
        Bracket,
        /// <summary>
        /// call Enumerable Ex Method
        /// </summary>
        CallMethod,
    }
}
