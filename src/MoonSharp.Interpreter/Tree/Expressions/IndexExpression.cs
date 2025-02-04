﻿using System;
using MoonSharp.Interpreter.DataStructs;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.Tree.Expressions
{
	class IndexExpression : Expression, IVariable
	{
		Expression m_BaseExp;
		Expression m_IndexExp;
		string m_Name;
		private bool inc;
		private bool dec;

		public bool IsAssignment => inc || dec;

		public IndexExpression(Expression baseExp, Expression indexExp, ScriptLoadingContext lcontext)
			: base(lcontext)
		{
			m_BaseExp = baseExp;
			m_IndexExp = indexExp;
			//inc/dec expr
			if (lcontext.Lexer.Current.Type == TokenType.Op_Inc)
			{
				inc = true;
				lcontext.Lexer.Next();
			} 
			else if (lcontext.Lexer.Current.Type == TokenType.Op_Dec)
			{
				dec = true;
				lcontext.Lexer.Next();
			}
		}

		public IndexExpression(Expression baseExp, string name, ScriptLoadingContext lcontext)
			: base(lcontext)
		{
			m_BaseExp = baseExp;
			m_Name = name;
			//inc/dec expr
			if (lcontext.Lexer.Current.Type == TokenType.Op_Inc)
			{
				inc = true;
				lcontext.Lexer.Next();
			} 
			else if (lcontext.Lexer.Current.Type == TokenType.Op_Dec)
			{
				dec = true;
				lcontext.Lexer.Next();
			}
		}


		public override void Compile(ByteCode bc)
		{
			m_BaseExp.Compile(bc);

			if (m_Name != null)
			{
				bc.Emit_Index(m_Name, true);
			}
			else if (m_IndexExp is LiteralExpression lit && lit.Value.Type == DataType.String)
			{
				bc.Emit_Index(lit.Value.String);
			}
			else
			{
				m_IndexExp.Compile(bc);
				bc.Emit_Index(isExpList: (m_IndexExp is ExprListExpression));
			}

			if (inc)
			{
				bc.Emit_Copy(0);
				bc.Emit_Literal(DynValue.NewNumber(1.0));
				bc.Emit_Operator(OpCode.Add);
				CompileAssignment(bc, Operator.NotAnOperator, 0, 0);
				bc.Emit_Pop();
			} 
			else if (dec)
			{
				bc.Emit_Copy(0);
				bc.Emit_Literal(DynValue.NewNumber(1.0));
				bc.Emit_Operator(OpCode.Sub);
				CompileAssignment(bc, Operator.NotAnOperator, 0, 0);
				bc.Emit_Pop();
			}
		}

		public void CompileAssignment(ByteCode bc, Operator op, int stackofs, int tupleidx)
		{
			if (op != Operator.NotAnOperator)
			{
				Compile(bc); //left
				bc.Emit_CopyValue(stackofs + 1, tupleidx); //right
				bc.Emit_Operator(BinaryOperatorExpression.OperatorToOpCode(op));
				stackofs = 0;
				tupleidx = 0;
			}
			m_BaseExp.Compile(bc);

			if (m_Name != null)
			{
				bc.Emit_IndexSet(stackofs, tupleidx, m_Name, isNameIndex: true);
			}
			else if (m_IndexExp is LiteralExpression lit && lit.Value.Type == DataType.String)
			{
				bc.Emit_IndexSet(stackofs, tupleidx, lit.Value.String);
			}
			else
			{
				m_IndexExp.Compile(bc);
				bc.Emit_IndexSet(stackofs, tupleidx, isExpList: (m_IndexExp is ExprListExpression));
			}

			if (op != Operator.NotAnOperator) bc.Emit_Pop();
		}

		public override DynValue Eval(ScriptExecutionContext context)
		{
			DynValue b = m_BaseExp.Eval(context).ToScalar();
			DynValue i = m_IndexExp != null ? m_IndexExp.Eval(context).ToScalar() : DynValue.NewString(m_Name);

			if (b.Type != DataType.Table) throw new DynamicExpressionException("Attempt to index non-table.");
			else if (i.IsNilOrNan()) throw new DynamicExpressionException("Attempt to index with nil or nan key.");
			return b.Table.Get(i);
		}

		public override bool EvalLiteral(out DynValue dv)
		{
			dv = DynValue.Nil;
			return false;
		}
	}
}
