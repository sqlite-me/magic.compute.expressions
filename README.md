# magic.compute.expressions
This library is a .net tool for convert string expresion to Delegate, and return one value.
using like :
var exp = new MExpression("{0} > {1} ? {0} - {1} : {1} - {0}");
var @delegate = exp.GetDelegate(new object[]{ 15, 20 },out object[] usedArgs);
var result = @delegate.DynamicInvoke(usedArgs);
more demos see the test
