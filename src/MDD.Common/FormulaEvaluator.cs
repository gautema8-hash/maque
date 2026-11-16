using System;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace MDD.Common;

public class FormulaEvaluator
{
	public static double EvaluateFormula(string formula, double price, double quantity = 0.0)
	{
		ParameterExpression parameterExpression = Expression.Parameter(typeof(double), "A");
		ParameterExpression parameterExpression2 = Expression.Parameter(typeof(double), "B");
		return ((Func<double, double, double>)DynamicExpressionParser.ParseLambda(new ParameterExpression[2] { parameterExpression, parameterExpression2 }, null, formula).Compile())(price, quantity);
	}
}
