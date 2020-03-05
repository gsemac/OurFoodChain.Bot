using OurFoodChain.Data.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Taxa {

    public interface IBulkOperation {

        ISearchQuery Query { get; }
        string OperationName { get; }
        IEnumerable<string> Arguments { get; }

    }

}