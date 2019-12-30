using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Taxa {

    public interface IBulkOperation {

        SearchQuery Query { get; }
        string OperationName { get; }
        IEnumerable<string> Arguments { get; }

    }

}