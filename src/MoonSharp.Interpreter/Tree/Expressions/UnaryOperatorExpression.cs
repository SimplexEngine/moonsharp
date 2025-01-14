﻿using System;
using MoonSharp.Interpreter.DataStructs;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.Tree.Expressions
{
	class UnaryOperatorExpression : Expression
	{
		Expression m_Exp;
		string m_OpText;
		private Token tok;

		public UnaryOperatorExpression(ScriptLoadingContext lcontext, Expression subExpression, Token unaryOpToken)
			: base(lcontext)
		{
			m_OpText = unaryOpToken.Text;
			tok = unaryOpToken;
			m_Exp = subExpression;
		}

		public bool IsNegativeNumber => m_Exp is LiteralExpression && m_OpText == "-";



		public override void Compile(ByteCode bc)
		{
			switch (m_OpText)
			{
				//prefix inc/dec operators - return number AFTER calculation
				case "++":
				{
					if (m_Exp is IVariable var)
					{
						m_Exp.Compile(bc);
						bc.Emit_Literal(DynValue.NewNumber(1.0));
						bc.Emit_Operator(OpCode.Add);
						//assignment doesn't pop
						var.CompileAssignment(bc, Operator.NotAnOperator, 0, 0);
					}
					else
						throw new SyntaxErrorException(tok, "'++' can only be used with indexers or variables",
							"++");
					break;
				}
				case "--":
				{
					if (m_Exp is IVariable var)
					{
						m_Exp.Compile(bc);
						bc.Emit_Literal(DynValue.NewNumber(1.0));
						bc.Emit_Operator(OpCode.Sub);
						var.CompileAssignment(bc, Operator.NotAnOperator, 0, 0);
					}
					else
						throw new SyntaxErrorException(tok, "'--' can only be used with indexers or variables",
							"--");
					break;
				}
				case "!":
				case "not":
					m_Exp.Compile(bc);
					bc.Emit_Operator(OpCode.Not);
					break;
				case "#":
					m_Exp.Compile(bc);
					bc.Emit_Operator(OpCode.Len);
					break;
				case "-":
					m_Exp.Compile(bc);
					bc.Emit_Operator(OpCode.Neg);
					break;
				default:
					throw new InternalErrorException("Unexpected unary operator '{0}'", m_OpText);
			}


		}

		public override DynValue Eval(ScriptExecutionContext context)
		{
			DynValue v = m_Exp.Eval(context).ToScalar();

			switch (m_OpText)
			{
				case "!":
				case "not":
					return DynValue.NewBoolean(!v.CastToBool());
				case "#":
					return v.GetLength();
				case "-":
					{
						double? d = v.CastToNumber();

						if (d.HasValue)
							return DynValue.NewNumber(-d.Value);

						throw new DynamicExpressionException("Attempt to perform arithmetic on non-numbers.");
					}
				default:
					throw new DynamicExpressionException("Unexpected unary operator '{0}'", m_OpText);
			}
		}

		public override bool EvalLiteral(out DynValue dv)
		{
			dv = DynValue.Nil;
			if (!m_Exp.EvalLiteral(out var v))
			{
				return false;
			}
			switch (m_OpText)
			{
				case "!":
				case "not":
					dv = DynValue.NewBoolean(!v.CastToBool());
					return true;
				case "#":
				case "++": 
				case "--":
					return false;
				case "-":
					double? d = v.CastToNumber();
					if (d.HasValue)
					{
						dv = DynValue.NewNumber(-d.Value);
						return true;
					}
					break;
			}
			//Could not evaluate literal - give runtime error later
			return false; 
		}
	}
}
