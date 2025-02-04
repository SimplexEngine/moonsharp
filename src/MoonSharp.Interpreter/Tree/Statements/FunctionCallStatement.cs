﻿using System;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;
using MoonSharp.Interpreter.Tree.Expressions;

namespace MoonSharp.Interpreter.Tree.Statements
{
	class FunctionCallStatement : Statement
	{
		FunctionCallExpression m_FunctionCallExpression;

		public FunctionCallStatement(ScriptLoadingContext lcontext, FunctionCallExpression functionCallExpression)
			: base(lcontext)
		{
			m_FunctionCallExpression = functionCallExpression;
			lcontext.Source.Refs.Add(m_FunctionCallExpression.SourceRef);
		}


		public override void Compile(ByteCode bc)
		{
			using (bc.EnterSource(m_FunctionCallExpression.SourceRef))
			{
				m_FunctionCallExpression.Compile(bc);
				bc.Emit_Pop();
				bc.SourceRefs[bc.SourceRefs.Count - 1] = null; //Remove breakpoint stop
			}
		}

		
	}
}
