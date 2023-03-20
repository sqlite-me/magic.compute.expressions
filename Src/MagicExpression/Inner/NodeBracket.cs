using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;

namespace MagicExpression.Inner
{
    internal class NodeBracket : NodeData
    {
        public NodeBracket(string allExpressoin, int startIndex)
            : base(allExpressoin, startIndex)
        {
        }
        public List<NodeData> NodeDatas { get; } = new List<NodeData>();

        public override bool NodeComplated => base.ExpClosed;
        public void SetExpClosed()
        {
            base.ExpClosed = true;
            processNodes();
        }

        private void processNodes()
        {
            var end = NodeDatas.Count - 1;
            for (var i=end; i>=0; i--)
            {
                switch(NodeDatas[i])
                {
                    case NodeOperatorSingle sNode:
                        if(sNode.Target == null&&i<end)
                        {
                            sNode.Target = NodeDatas[i+1];
                            NodeDatas.RemoveAt(i+1);
                        }
                        break;
                    case NodeOperatorType sNode:
                        if (sNode.Target == null)
                        {
                            if (i < end&&sNode.KeyWord=="()")
                            {
                                sNode.Target = NodeDatas[i + 1];
                                NodeDatas.RemoveAt(i + 1);
                            }
                            else if(i>0)
                            {
                                sNode.Target = NodeDatas[i - 1];
                                NodeDatas.RemoveAt(i - 1);
                                i--;
                            }
                        }
                        break;
                }
            }

            ProcessConvertNode(this.NodeDatas,true);

            var operators = new string[] { "??" };
            DoubleOperatorToTree(NodeDatas, operators);

            operators = new string[] { "|", "&", "^", ">>", "<<" };
            DoubleOperatorToTree(NodeDatas, operators);

            operators = new string[] { "*", "/", "%" };
            DoubleOperatorToTree(NodeDatas, operators);

            operators = new string[] { "+", "-" };
            DoubleOperatorToTree(NodeDatas, operators);

            operators = new string[] { ">", "<", ">=", "<=", "==", "!=" };
            DoubleOperatorToTree(NodeDatas, operators);

            operators = new string[] { "&&", "||" };
            DoubleOperatorToTree(NodeDatas, operators);

            for (var i= 0;i< NodeDatas.Count; i++)
            {
                if (NodeDatas[i] is NodeOperatorTernary trNode)
                {
                    if(trNode.First==null)
                    {
                        if (i == 0) throw new ExpressionErrorException(trNode.StartIndex, trNode.OrginalStr);
                        trNode.First = NodeDatas[i-1];
                        NodeDatas.RemoveAt(i - 1);
                        i--;
                    }
                    if (trNode.Third == null)
                    {
                        if (i >= NodeDatas.Count-1) throw new ExpressionErrorException(trNode.EndIndex, trNode.OrginalStr);
                        trNode.Third = NodeDatas[i + 1];
                        NodeDatas.RemoveAt(i + 1);

                    }
                }
            }
        }

        private static void ProcessConvertNode(List<NodeData> listNodes, bool isEnd)
        {
            if (listNodes.Count < 2) return;
            var last = listNodes[listNodes.Count - 1];

            var typeLast = isEnd ? 2 : 3;
            if (listNodes.Count >= typeLast)// process type convert
            {
                switch (last)
                {
                    default:
                        while (listNodes.Count >= typeLast &&
                            listNodes[listNodes.Count - typeLast] is NodeOperatorType tNode && tNode.ExpClosed
                            && listNodes[listNodes.Count - typeLast + 1] is NodeData node && node.NodeComplated)
                        {
                            CheckNodeEnd(node);
                            tNode.Target = node;
                            listNodes.RemoveAt(listNodes.Count - typeLast + 1);
                        }
                        break;
                    case NodeOperatorAccess _:
                        break;
                }
            }
            if (isEnd)
                CheckNodeEnd(listNodes.ToArray());
        }

        private static void CheckNodeEnd(params NodeData[] nodeDatas)
        {
            if (nodeDatas == null || nodeDatas.Length == 0) return;
            foreach (var one in nodeDatas)
            {
                switch(one)
                {
                    case NodeCallMethod cNode:
                        cNode.SetNodeComplated();
                        break;
                }
            }
        }
        
        private static void DoubleOperatorToTree(List<NodeData> listNodes, string[] operators)
        {
            int len = listNodes.Count;
            for (int i = 0; i < len; i++)
            {
                switch (listNodes[i])
                {
                    case NodeOperatorDouble @double:
                        if (operators.Contains(@double.KeyWord) && @double.Left==null&&@double.Right==null)
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
        public void AddNode(NodeData nodeData)
        {
            NodeDatas.Add(nodeData);
        }
        public override Expression GetExpression()
        {
            throw new ExpressionErrorException(StartIndex,OrginalStr);
        }
    }
}
