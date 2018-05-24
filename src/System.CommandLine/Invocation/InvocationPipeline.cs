// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.CommandLine.Invocation
{
    internal class InvocationPipeline
    {
        private readonly ParseResult parseResult;

        public InvocationPipeline(ParseResult parseResult)
        {
            this.parseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
        }

        public async Task<int> InvokeAsync(IConsole console)
        {
            var context = new InvocationContext(parseResult, console);

            var invocations = new List<InvocationMiddleware>(parseResult.Parser.Configuration.InvocationList);

            invocations.Add(async (invocationContext, next) => {
                var handler = invocationContext.ParseResult
                                               .CommandDefinition()
                                               .ExecutionHandler;

                if (handler != null)
                {
                    await handler.InvokeAsync(parseResult);
                    context.ResultCode = 0;
                }
            });

            var invocationChain = invocations.Aggregate(
                (first, second) =>
                    ((ctx, next) =>
                        first(ctx,
                              c => second(c, next))));

            await invocationChain(context, invocationContext => Task.CompletedTask);

            context.InvocationResult?.Apply(context);

            return context.ResultCode;
        }
    }
}