﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueNodeFactoryImpl : DbgEngineValueNodeFactory {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;
		readonly DbgDotNetFormatter formatter;

		public DbgEngineValueNodeFactoryImpl(DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetFormatter formatter) {
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		}

		public override DbgEngineValueNode Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}

		public override void Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNode> callback, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}

		public override DbgEngineValueNode[] Create(DbgEvaluationContext context, DbgStackFrame frame, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.Invoke(() => CreateCore(context, frame, objectIds, options, cancellationToken));

		public override void Create(DbgEvaluationContext context, DbgStackFrame frame, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.BeginInvoke(() => callback(CreateCore(context, frame, objectIds, options, cancellationToken)));

		DbgEngineValueNode[] CreateCore(DbgEvaluationContext context, DbgStackFrame frame, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			DbgDotNetValue objectIdValue = null;
			var res = new DbgEngineValueNode[objectIds.Length];
			try {
				var output = ObjectCache.AllocDotNetTextOutput();
				for (int i = 0; i < res.Length; i++) {
					var objectId = (DbgEngineObjectIdImpl)objectIds[i];
					var dnObjectId = objectId.DotNetObjectId;
					objectIdValue = objectId.Runtime.GetValue(context, dnObjectId, cancellationToken);

					formatter.FormatObjectIdName(context, output, dnObjectId.Id);
					var name = output.CreateAndReset();
					var expression = name.ToString();

					res[i] = valueNodeFactory.Create(context, frame, name, objectIdValue, options, expression, PredefinedDbgValueNodeImageNames.ObjectId, true, false, objectIdValue.Type, cancellationToken);
				}
				ObjectCache.Free(ref output);
				return res;
			}
			catch {
				context.Process.DbgManager.Close(res.Where(a => a != null));
				objectIdValue?.Dispose();
				throw;
			}
		}
	}
}