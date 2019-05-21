using System;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace EshopWorld.Tools.Tests
{
    public static class StringAssertionsExtensions
    {
        public static AndConstraint<StringAssertions> BeValidReverseProxyUrl(this StringAssertions parent)
        {
            var uri = new Uri(parent.Subject);
            Execute.Assertion.ForCondition(uri.Host.Equals("localhost", StringComparison.Ordinal))
                .FailWith("invalid hostname - localhost expected");

            Execute.Assertion
                .ForCondition(uri.Scheme.Equals("https", StringComparison.Ordinal) ||
                              uri.Scheme.Equals("http", StringComparison.Ordinal))
                .FailWith("invalid scheme - expected 'http' or 'https'");

            return new AndConstraint<StringAssertions>(parent);
        }
    }
}
