﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MagicExpression.Inner
{
    internal abstract class NodeOperator : NodeData
    {
        public NodeOperator(string allExpressoin, int startIndex, int endIndex=-1)
            : base( allExpressoin, startIndex, endIndex)
        {
        }
    }
}
