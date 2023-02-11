using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using MagicExpression.Inner;

namespace MagicExpression
{
    public class MExpression
    {

        /// <summary>
        /// 匹配 字符串、true、false、null、is、is not、as、.[Property]、{\d}
        /// </summary>
        private static Regex regex = new Regex("(?<!\\\\)\".*?(?<!\\\\)\"|true|false|null|is\\s+not|is|as|\\?\\?|\\?|(\\.\\s*[_|a-z|A-Z]\\w*)|{-?\\d+}");
        private static Regex constNumberRegex = new Regex("((\\d+\\.?\\d*)|(\\d*\\.\\d+))((d|f|m|ul|lu|l|u)?)", RegexOptions.IgnoreCase);
        private List<Match> matchsList;
        private List<Match> matchsNumList;
        private readonly NodeBracket root;
        private readonly Dictionary<int, Param> nodeParams;
        private readonly Dictionary<string,Delegate> dicExistsDelegate=new Dictionary<string,Delegate>();
        private readonly List<NodeOperatorType> typeOperatorNodes = new List<NodeOperatorType>();
        private readonly List<NodeOperatorType> hasVariableTypeNode = new List<NodeOperatorType>();
        public IReadOnlyList<int> ArgsIndexs{get;private set;}

        public MExpression(string expressionStr)
        {
            matchsList = regex.Matches(expressionStr).ToList();
            matchsNumList = new List<Match>();
            nodeParams = new Dictionary<int, Param>();
            foreach (Match one in constNumberRegex.Matches(expressionStr))
            {
                var end = one.Index + one.Length - 1;
                var has = matchsList.Any(t =>
                {
                    var tEnd = t.Index + t.Length - 1;
                    return (one.Index >= t.Index && one.Index <= tEnd) || (end >= t.Index && end <= tEnd);
                });
                if (has) { continue; }
                matchsNumList.Add(one);
            }
            if (matchsNumList.Count > 0)
            {
                matchsList.AddRange(matchsNumList);
            }
            root = new NodeBracket(expressionStr, 0);
            parseBrackets(root, expressionStr);
            toTree(root);
            ArgsIndexs= nodeParams.Keys.OrderBy(t=>t).ToList();
        }
        private void parseBrackets(NodeBracket nodeBracket, string? str = null)
        {
            var opretorChars = new char[] { '=', '>', '<', '|', '&', '^', '+', '-', '*', '/', '%' };
            var dicStrs = matchsList.ToDictionary(t => t.Index, t => t);
            var dicVariableNames = new Dictionary<int, NodeOperatorType>();
            Stack<NodeBracket> bracketNotClosedStack = new Stack<NodeBracket>();
            Stack<NodeBracket> arrowBracketNotClosedStack = new Stack<NodeBracket>();// 尖括弧
            str ??= nodeBracket.OrginalStr[1..^1];
            var stackTop = nodeBracket;
            for (var i = 0; i < str.Length; i++)
            {
                if (dicStrs.ContainsKey(i))
                {
                    processArrowAsLessThan();
                    var match = dicStrs[i];
                    var upVal = match.Value;
                    switch (upVal)
                    {
                        case "true":
                            stackTop.AddNode(new NodeConst(true, match.Value, i));
                            break;
                        case "false":
                            stackTop.AddNode(new NodeConst(false, match.Value, i));
                            break;
                        case "null":
                            stackTop.AddNode(new NodeConst(null, match.Value, i));
                            break;
                        case "is":
                        case "as":
                            var typeName = parseType(str, match.Index + match.Length, out int len);
                            i += len;
                            var typeOperator = new NodeOperatorType(upVal, typeName, str.Substring(match.Index, match.Length + len), match.Index);
                            stackTop.AddNode(typeOperator);
                            typeOperatorNodes.Add(typeOperator);
                            break;
                        case "?":
                            stackTop.AddNode(new NodeOperatorTernary("?", match.Value, i));
                            break;
                        case "??":
                            stackTop.AddNode(new NodeOperatorDouble("??", i));
                            break;
                        default:
                            if (upVal.StartsWith("is ") && upVal.Replace(" ", "") == "isnot")// is not
                            {
                                typeName = parseType(str, match.Index + match.Length, out len);
                                i += len;
                                typeOperator = new NodeOperatorType("isnot", typeName, str.Substring(match.Index, match.Length + len), match.Index);
                                stackTop.AddNode(typeOperator);
                                typeOperatorNodes.Add(typeOperator);
                            }
                            else if (upVal[0] == '\"')//string
                            {
                                stackTop.AddNode(new NodeConst(match.Value.Trim('\"'), match.Value, i));
                            }
                            else if (upVal[0] == '{')//param
                            {
                                var index = int.Parse(upVal[1..^1].Trim());
                                var param = nodeParams.ContainsKey(index) ? nodeParams[index] : nodeParams[index] = new Param(index);
                                var paramNode = new NodeParam(index, param, match.Value, i);
                                param.ParamNodes.Add(paramNode);
                                stackTop.AddNode(paramNode);
                            }
                            else if (upVal[0] == '.' && (upVal[1] == ' ' || upVal[1] == '_' || (upVal[1] >= 'a' && upVal[1] <= 'z') || (upVal[1] >= 'A' && upVal[1] <= 'Z')))
                            {//.Property
                                stackTop.AddNode(new NodeOperatorAccess(match.Value.Trim(' ', '.'), match.Value, i));
                            }
                            else//num
                            {
                                stackTop.AddNode(new NodeConst(stringToNumConst(match.Value), match.Value, i));
                            }
                            break;
                    }
                    i += match.Length - 1;
                    continue;
                }
                if (dicVariableNames.ContainsKey(i))
                {
                    var isType = dicVariableNames[i];
                    stackTop.AddNode(new NodeVariable(isType, i));
                    i += isType.VariableName.Length - 1;
                    continue;
                }

                switch (str[i])
                {
                    case ' ':
                        continue;
                    case '(':
                        processArrowAsLessThan();
                        NodeBracket notClosed = new NodeBracket(null!, i) { Parent = stackTop };
                        bracketNotClosedStack.Push(notClosed);
                        stackTop = notClosed;
                        break;
                    case ')':
                        if (bracketNotClosedStack.Count > 0)
                        {
                            notClosed = bracketNotClosedStack.Pop();
                            var bracketChild = new NodeBracket(str.Substring(notClosed.OrignalIndex, i - notClosed.OrignalIndex + 1), i);
                            if (notClosed.NodeDatas.Count > 0)
                            {
                                bracketChild.NodeDatas.AddRange(notClosed.NodeDatas);
                            }
                            if (notClosed.BracketNodes.Count > 0)
                            {
                                bracketChild.BracketNodes.AddRange(notClosed.BracketNodes);
                            }
                            stackTop = (NodeBracket)notClosed.Parent!;
                            stackTop.AddBracketNode(bracketChild);
                            if (notClosed.NodeDatas.Count == 2 && notClosed.NodeDatas[1] is NodeOperatorType nodeType)
                            {
                                switch (nodeType.Operator)
                                {
                                    case "is":
                                    case "isnot":
                                        var iStart = nodeType.OrignalIndex + nodeType.OrginalStr.Length;
                                        if (iStart < i)
                                        {
                                            var varName = str.Substring(iStart, i - iStart).Trim();
                                            if (varName != "")
                                            {
                                                nodeType.VariableName = varName;
                                                foreach (Match one in Regex.Matches(str.Substring(i + 1), "\\b" + varName + "\\b"))
                                                {
                                                    dicVariableNames.Add(i + 1 + one.Index, nodeType);
                                                }
                                                hasVariableTypeNode.Add(nodeType);
                                            }
                                        }
                                        ;
                                        break;
                                }
                            }
                        }
                        else
                        {
                            throw new ApplicationException("error");
                        }
                        break;
                    case '!':
                        processArrowAsLessThan();
                        if (str[i + 1] == '=')
                        {
                            stackTop.AddNode(new NodeOperatorDouble("!=", i));
                            i++;
                        }
                        else
                            stackTop.AddNode(new NodeOperatorSingle("!", i));
                        break;
                    case '~':
                        processArrowAsLessThan();
                        stackTop.AddNode(new NodeOperatorSingle("~", i));
                        break;
                    case '<':
                        if (str[i + 1] == '<' || str[i + 1] == '=')
                        {
                            processArrowAsLessThan();
                            stackTop.AddNode(new NodeOperatorDouble(str.Substring(i, 2), i));
                            i++;
                        }
                        else
                        {
                            arrowBracketNotClosedStack.Push(new NodeBracket(null, i) { Parent = stackTop });
                        }
                        break;
                    case '>':
                        if (str[i + 1] == '>' || str[i + 1] == '=')
                        {
                            processArrowAsLessThan();
                            stackTop.AddNode(new NodeOperatorDouble(str.Substring(i, 2), i));
                            i++;
                        }
                        else if(arrowBracketNotClosedStack.Count == 0)//>
                        {
                            stackTop.AddNode(new NodeOperatorDouble(str[i].ToString(), i));
                        }
                        else
                        {
                            arrowBracketNotClosedStack.Pop();//泛型
                        }
                        break;
                    case ':':
                        processArrowAsLessThan();
                        stackTop.AddNode(new NodeOperatorDouble(":", i));// 为三目运算准备
                        break;
                    default:
                        if (opretorChars.Contains(str[i]))
                        {
                            processArrowAsLessThan();
                            var opt = new StringBuilder();
                            opt.Append(str[i]);
                            if (opretorChars.Contains(str[i + 1]))
                            {
                                opt.Append(str[i + 1]);
                                i++;
                            }
                            stackTop.AddNode(new NodeOperatorDouble(opt.ToString(), i));
                        }
                        else
                        {
                        }
                        break;
                }
            }

            if (bracketNotClosedStack.Count > 0)
                throw new ApplicationException("need close bracket )");
            if (arrowBracketNotClosedStack.Count > 0)
                throw new ApplicationException("need close angle bracket >");

            void processArrowAsLessThan()
            {
                if (arrowBracketNotClosedStack.Count > 0)
                {
                    var arrowBracket = arrowBracketNotClosedStack.Pop();
                    stackTop.AddNode(new NodeOperatorDouble("<", arrowBracket.OrignalIndex));
                }
            }
        }
        private void toTree(NodeBracket nodeBracket)
        {
            int len;
            if (nodeBracket.BracketNodes?.Count > 0)
            {
                len = nodeBracket.BracketNodes.Count;
                for (var i = 0; i < len; i++)
                {
                    var one = nodeBracket.BracketNodes[i];
                    if (one.NodeDatas.Count == 0)// 类型转换
                    {
                        var iD = nodeBracket.NodeDatas.IndexOf(one);
                        if (nodeBracket.NodeDatas[iD] is NodeOperatorType typeOperator && typeOperatorNodes.Contains(typeOperator))
                        {
                            typeOperatorNodes.Remove(typeOperator);
                        }
                        nodeBracket.NodeDatas[iD] = typeOperator =
                            new NodeOperatorType("()", one.OrginalStr, one.OrginalStr, one.OrignalIndex);
                        typeOperatorNodes.Add(typeOperator);
                        nodeBracket.BracketNodes.RemoveAt(i);
                        i--; len--;
                    }
                    else
                    {
                        toTree(one);
                    }
                }
            }

            for (int i = nodeBracket.NodeDatas.Count - 1; i > -1; i--)
            {
                switch (nodeBracket.NodeDatas[i])
                {
                    case NodeOperatorAccess access:
                        access.Target = nodeBracket.NodeDatas[i - 1];
                        nodeBracket.NodeDatas.RemoveAt(i - 1);
                        i--;
                        break;
                    case NodeOperatorSingle single:
                        single.Target = nodeBracket.NodeDatas[i + 1];
                        nodeBracket.NodeDatas.RemoveAt(i + 1);
                        break;
                    case NodeOperatorType opType:
                        if (opType.Operator == "()")
                        {
                            opType.Target = nodeBracket.NodeDatas[i + 1];
                            nodeBracket.NodeDatas.RemoveAt(i + 1);
                        }
                        else
                        {
                            opType.Target = nodeBracket.NodeDatas[i - 1];
                            nodeBracket.NodeDatas.RemoveAt(i - 1);
                            i--;
                        }
                        break;
                }
            }

            var operators = new string[] { "??"};
            DoubleOperatorToTree(nodeBracket, operators);

            operators = new string[] { "|", "&", "^", ">>", "<<" };
            DoubleOperatorToTree(nodeBracket, operators);

            operators = new string[] { "*", "/", "%" };
            DoubleOperatorToTree(nodeBracket, operators);

            operators = new string[] { "+", "-" };
            DoubleOperatorToTree(nodeBracket, operators);

            operators = new string[] { ">", "<" , ">=", "<=", "==", "!=" };
            DoubleOperatorToTree(nodeBracket, operators);

            operators = new string[] { "&&", "||" };
            DoubleOperatorToTree(nodeBracket, operators);

            len = nodeBracket.NodeDatas.Count;
            for (int i = 0; i < len; i++)
            {
                switch (nodeBracket.NodeDatas[i])
                {
                    case NodeOperatorTernary ternary:
                        if (!(nodeBracket.NodeDatas[i + 2] is NodeOperatorDouble tmp) || tmp.Operator != ":")
                        {
                            throw new ApplicationException();
                        }
                        ternary.First = nodeBracket.NodeDatas[i - 1];
                        ternary.Second = nodeBracket.NodeDatas[i + 1];
                        ternary.Third = nodeBracket.NodeDatas[i + 3];
                        nodeBracket.NodeDatas.RemoveAt(i + 3);
                        nodeBracket.NodeDatas.RemoveAt(i + 2);
                        nodeBracket.NodeDatas.RemoveAt(i + 1);
                        nodeBracket.NodeDatas.RemoveAt(i - 1);
                        len -= 4;
                        i--;
                        break;
                }
            }
        }

        private static void DoubleOperatorToTree(NodeBracket nodeBracket, string[] operators)
        {
            int len = nodeBracket.NodeDatas.Count;
            for (int i = 0; i < len; i++)
            {
                switch (nodeBracket.NodeDatas[i])
                {
                    case NodeOperatorDouble @double:
                        if (operators.Contains(@double.Operator)&&@double.Operator!=":")
                        {
                            @double.Left = nodeBracket.NodeDatas[i - 1];
                            @double.Right = nodeBracket.NodeDatas[i + 1];
                            nodeBracket.NodeDatas.RemoveAt(i + 1);
                            nodeBracket.NodeDatas.RemoveAt(i - 1);
                            i--;
                            len -= 2;
                        }
                        break;
                }
            }
        }

        private string parseType(string str, int i, out int len)
        {
            var tmp = str[i..];
            var match = Regex.Match(tmp, @"^\s*[\w|_][\w|_|\d|\.|<|>]*");
            var strTypeName = match.Value.Trim();
            var match1 = matchsList.FirstOrDefault(t => t.Value == strTypeName);
            if (match1 != null)
            {
                matchsList.Remove(match1);
            }
            len = match.Length;
            return strTypeName;
        }

        private object stringToNumConst(string value)
        {
            char? last = value.Length > 0 ? (char?)value[^1] : null!;
            char? lastSecond = value.Length > 1 ? (char?)value[^2] : null;
            string type;
            string val;
            if (lastSecond.HasValue && ((lastSecond > 'a' && lastSecond < 'z') || (lastSecond > 'A' && lastSecond < 'Z')))
            {
                val = value[..^2];
                type = new string(new[] { lastSecond.Value, last.Value });
            }
            else if (last.HasValue && ((last > 'a' && last < 'z') || (last > 'A' && last < 'Z')))
            {
                val = value[..^1];
                type = new string(new[] { last.Value });
            }
            else if (value.Contains('.'))
            {
                val = value;
                type = "D";
            }
            else
            {
                val = value;
                type = "";
            }
            switch (type)
            {
                case "D":
                case "d":
                    return double.Parse(val);
                case "F":
                case "f":
                    return float.Parse(val);
                case "M":
                case "m":
                    return decimal.Parse(val);
                case "UL":
                case "LU":
                case "ul":
                case "lu":
                case "Ul":
                case "Lu":
                case "uL":
                case "lU":
                    return ulong.Parse(val);
                case "L":
                case "l":
                    return long.Parse(val);
                case "U":
                case "u":
                    return uint.Parse(val);
                default:
                    return int.Parse(val);
            }
        }

        public Delegate GetDelegate(IList<object> args, out object[] usedArgs)
        {
            var key = args?.Count > 0 ? string.Join(",", args.Select(t => t == null ? "null" : t.GetType().FullName)):"";
            if (dicExistsDelegate.ContainsKey(key))
            {
                if(ArgsIndexs?.Count> 0)
                {
                    usedArgs= new object[ArgsIndexs.Count];
                    for (var i=0;i< ArgsIndexs.Count;i++)
                    {
                        usedArgs[i] = args[ArgsIndexs[i]];
                    }
                }
                else
                {
                    usedArgs= new object[0];
                }
                return dicExistsDelegate[key];
            }

            hasVariableTypeNode.ForEach(o => o.ClearVariable());
            var tempArgs = new List<object>();
            List<ParameterExpression> parameters = new List<ParameterExpression>();
            foreach (var i in nodeParams.Keys.OrderBy(t => t))
            {
                var one = nodeParams[i];
                tempArgs.Add(args[i]);
                var type = args[i]?.GetType();
                one.SetParameter(type);
                parameters.Add(one.Parameter);
            }

            if (typeOperatorNodes.Count > 0 && args?.Count > 0)
            {
                var temp = args.Where(t => t != null).ToArray();
                if (temp.Length > 0)
                {
                    var assemblysMayUsed = Type.GetTypeArray(temp).Select(t => t.Assembly).Distinct().ToArray();
                    typeOperatorNodes.ForEach(one => one.AssemblysMayUsed = assemblysMayUsed);
                }
            }

            var express = root.GetExpression();
            var body = express;
            if (hasVariableTypeNode.Count > 0)
            {
                List<ParameterExpression> variables = new List<ParameterExpression>();
                List<Expression> bodys = new List<Expression>();
                foreach (var one in hasVariableTypeNode)
                {
                    variables.Add(one.GetVariableExpression());
                    bodys.Add(one.GetAssain());
                }
                bodys.Add(express);
                body = Expression.Block(variables, bodys);
            }
            var lambda = Expression.Lambda(body, parameters.ToArray());
            var @delegate = lambda.Compile();
            usedArgs = tempArgs.ToArray();
            dicExistsDelegate[key] = @delegate;
            return @delegate;
        }
    }
}