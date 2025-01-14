﻿using System.Collections.Generic;
using MoonSharp.Interpreter.DataStructs;
using MoonSharp.Interpreter.Execution;

namespace MoonSharp.Interpreter.Tree.Expressions
{
	class TableConstructor : Expression 
	{
		bool m_Shared = false;
		List<Expression> m_PositionalValues = new List<Expression>();
		List<KeyValuePair<Expression, Expression>> m_CtorArgs = new List<KeyValuePair<Expression, Expression>>();

		public TableConstructor(ScriptLoadingContext lcontext, bool shared)
			: base(lcontext)
		{
			m_Shared = shared;

			// here lexer is at the '{' (or '[' for c-like), go on
			TokenType closeType = TokenType.Brk_Close_Curly;
			if (lcontext.Syntax != ScriptSyntax.Lua && lcontext.Lexer.Current.Type == TokenType.Brk_Open_Square) {
				closeType = TokenType.Brk_Close_Square;
				lcontext.Lexer.Next();
			}
			else {
				CheckTokenType(lcontext, TokenType.Brk_Open_Curly, TokenType.Brk_Open_Curly_Shared);
			}

			while (lcontext.Lexer.Current.Type != closeType)
			{
				switch (lcontext.Lexer.Current.Type)
				{
					case TokenType.String:
						if (lcontext.Syntax != ScriptSyntax.Lua)
						{
							Token assign = lcontext.Lexer.PeekNext();
							if(assign.Type == TokenType.Colon)
								StructField(lcontext);
							else
								ArrayField(lcontext);
						}
						else ArrayField(lcontext);
						break;
					case TokenType.Name:
						{
							Token assign = lcontext.Lexer.PeekNext();

							if (assign.Type == TokenType.Op_Assignment ||
							    assign.Type == TokenType.Colon && lcontext.Syntax != ScriptSyntax.Lua)
							    StructField(lcontext);
							else
								ArrayField(lcontext);
						}
						break;
					case TokenType.Brk_Open_Square:
						MapField(lcontext);
						break;
					default:
						ArrayField(lcontext);
						break;
				}

				Token curr = lcontext.Lexer.Current;

				if (curr.Type == TokenType.Comma || curr.Type == TokenType.SemiColon)
				{
					lcontext.Lexer.Next();
				}
				else
				{
					break;
				}
			}

			CheckTokenType(lcontext, closeType);
		}

		private void MapField(ScriptLoadingContext lcontext)
		{
			lcontext.Lexer.SavePos();
			lcontext.Lexer.Next(); // skip '['

			Expression key = Expr(lcontext);
			if (lcontext.Syntax != ScriptSyntax.Lua &&
			    lcontext.Lexer.Current.Type == TokenType.Comma) {
				lcontext.Lexer.RestorePos();
				ArrayField(lcontext);
				return;
			}
			CheckTokenType(lcontext, TokenType.Brk_Close_Square);
			if (lcontext.Syntax != ScriptSyntax.Lua &&
			    lcontext.Lexer.Current.Type != TokenType.Op_Assignment &&
			    lcontext.Lexer.Current.Type != TokenType.Colon)
			{
				lcontext.Lexer.RestorePos();
				ArrayField(lcontext);
				return;
			}

			CheckTokenTypeEx(lcontext, TokenType.Op_Assignment, TokenType.Colon);

			Expression value = Expr(lcontext);

			m_CtorArgs.Add(new KeyValuePair<Expression, Expression>(key, value));
		}

		private void StructField(ScriptLoadingContext lcontext)
		{
			Expression key = new LiteralExpression(lcontext, DynValue.NewString(lcontext.Lexer.Current.Text));
			lcontext.Lexer.Next();

			CheckTokenTypeEx(lcontext, TokenType.Op_Assignment, TokenType.Colon);

			Expression value = Expr(lcontext);

			m_CtorArgs.Add(new KeyValuePair<Expression, Expression>(key, value));
		}


		private void ArrayField(ScriptLoadingContext lcontext)
		{
			Expression e = Expr(lcontext);
			m_PositionalValues.Add(e);
		}


		public override void Compile(Execution.VM.ByteCode bc)
		{
			bc.Emit_NewTable(m_Shared);

			foreach (var kvp in m_CtorArgs)
			{
				kvp.Key.Compile(bc);
				kvp.Value.Compile(bc);
				bc.Emit_TblInitN();
			}

			for (int i = 0; i < m_PositionalValues.Count; i++ )
			{
				m_PositionalValues[i].Compile(bc);
				bc.Emit_TblInitI(i == m_PositionalValues.Count - 1);
			}
		}


		public override DynValue Eval(ScriptExecutionContext context)
		{
			if (!this.m_Shared)
			{
				throw new DynamicExpressionException("Dynamic Expressions cannot define new non-prime tables.");
			}

			DynValue tval = DynValue.NewPrimeTable();
			Table t = tval.Table;

			int idx = 0;
			foreach (Expression e in m_PositionalValues)
			{
				t.Set(++idx, e.Eval(context));
			}

			foreach (KeyValuePair<Expression, Expression> kvp in this.m_CtorArgs)
			{
				t.Set(kvp.Key.Eval(context), kvp.Value.Eval(context));
			}

			return tval;
		}

		public override bool EvalLiteral(out DynValue dv)
		{
			dv = DynValue.Nil;
			return false;
		}
	}
}
