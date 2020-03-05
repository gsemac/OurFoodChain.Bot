using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Queries;

namespace OurFoodChain.Taxa {

    public class BulkOperation :
        IBulkOperation {

        public ISearchQuery Query { get; }
        public string OperationName { get; }
        public IEnumerable<string> Arguments { get; }

        public BulkOperation(string operationString) {

            // operation strings are of the form 
            // <query> > <operation> <arguments>

            int splitIndex = operationString.LastIndexOf('>');

            if (splitIndex < 0)
                throw new ArgumentException("The operation string is malformed.", nameof(operationString));

            string query = operationString.Substring(0, splitIndex);
            string operation = operationString.Substring(splitIndex + 1);

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query string cannot be empty.", nameof(operationString));

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Operation string cannot be empty.", nameof(operationString));

            Query = new SearchQuery(query);
            OperationName = operation.GetFirstWord().ToLowerInvariant();
            Arguments = StringUtilities.ParseArguments(operation.SkipWords(1));

        }

    }

}