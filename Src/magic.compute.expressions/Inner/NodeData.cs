using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace magic.compute.expressions.Inner
{
    internal abstract class NodeData
    {
        public NodeData(string allExpressoin, int startIndex,int endIndex=-1)
        {
            if (endIndex==-1) endIndex = startIndex;
            if (endIndex < startIndex) throw new ExpressionErrorException(startIndex,"");
            this.allExpressoin = allExpressoin;
            StartIndex = startIndex;
            EndIndex = endIndex;
            KeyWord = OrginalStr;
        }
        private string allExpressoin;
        public string KeyWord { get; set; }
        public int StartIndex { get; }
        public int EndIndex { get; set; }
        public string OrginalStr => EndIndex >= StartIndex ? allExpressoin.Substring(StartIndex, EndIndex - StartIndex + 1) : "";

        public virtual bool ExpClosed
        {
            get => _expClosed; set
            {
                if (_expClosed)
                    throw new Exception("it's complated");

                _expClosed = value;
            }
        }
        private bool _expClosed;

        public abstract bool NodeComplated { get; }

        public abstract Expression GetExpression();



        public static TypeCode CheckAndConvertToMax(ref Expression left, ref Expression right)
        {
            var leftTypeCode = Type.GetTypeCode(left.Type);
            var rightTypeCode = Type.GetTypeCode(right.Type);
            if (leftTypeCode == rightTypeCode)
            {
                return leftTypeCode;
            }
            else if (leftTypeCode > rightTypeCode && leftTypeCode < TypeCode.DateTime)
            {
                right = Expression.Convert(right, left.Type);
            }
            else if (leftTypeCode < rightTypeCode && rightTypeCode < TypeCode.DateTime)
            {
                left = Expression.Convert(left, right.Type);
            }

            return leftTypeCode > rightTypeCode ? leftTypeCode : rightTypeCode;
        }

        protected static void DoubleOperatorToTree(List<NodeData> listNodes)
        {
            var operators = new string[] { "??" };
            DoubleOperatorToTree(listNodes, operators);

            operators = new string[] { "|", "&", "^", ">>", "<<" };
            DoubleOperatorToTree(listNodes, operators);

            operators = new string[] { "*", "/", "%" };
            DoubleOperatorToTree(listNodes, operators);

            operators = new string[] { "+", "-" };
            DoubleOperatorToTree(listNodes, operators);

            operators = new string[] { ">", "<", ">=", "<=", "==", "!=" };
            DoubleOperatorToTree(listNodes, operators);

            operators = new string[] { "&&", "||" };
            DoubleOperatorToTree(listNodes, operators);

        }
        private static void DoubleOperatorToTree(List<NodeData> listNodes, string[] operators)
        {
            int len = listNodes.Count;
            for (int i = 0; i < len; i++)
            {
                switch (listNodes[i])
                {
                    case NodeOperatorDouble @double:
                        if (operators.Contains(@double.KeyWord) && @double.Left == null && @double.Right == null)
                        {
                            @double.Left = listNodes[i - 1];
                            @double.Right = listNodes[i + 1];
                            listNodes.RemoveAt(i + 1);
                            listNodes.RemoveAt(i - 1);
                            i--;
                            len -= 2;
                        }
                        break;
                }
            }
        }
    }
}
