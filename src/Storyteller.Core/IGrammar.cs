﻿using System.Collections;
using Storyteller.Core.Conversion;
using Storyteller.Core.Model;

namespace Storyteller.Core
{
    public interface IGrammar
    {
        IExecutionStep CreatePlan(Step step, FixtureLibrary library);

        GrammarModel Compile(Conversions conversions);
    }
}