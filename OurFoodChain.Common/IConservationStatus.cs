﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IConservationStatus :
        IExtinctionInfo {

        bool IsExinct { get; }

    }

}