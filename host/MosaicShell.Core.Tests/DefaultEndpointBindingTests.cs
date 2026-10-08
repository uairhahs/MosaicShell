using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class DefaultEndpointBindingTests
    {
        private sealed class Endpoint(int id)
        {
            public int Id { get; } = id;
        }

        [Fact]
        public void Endpoint_missing_at_construction_is_resolved_on_first_later_use()
        {
            // Log-on: the audio service starts before any render endpoint exists.
            Endpoint? available = null;
            using DefaultEndpointBinding<Endpoint> binding = new(() => available);

            _ = binding.Run(e => e.Id, -1).Should().Be(-1);

            available = new Endpoint(7);
            _ = binding.Run(e => e.Id, -1).Should().Be(7);
        }

        [Fact]
        public void Resolver_that_throws_is_treated_as_no_endpoint()
        {
            int calls = 0;
            using DefaultEndpointBinding<Endpoint> binding = new(() =>
            {
                calls++;
                if (calls == 1)
                {
                    throw new InvalidOperationException("audio service not ready");
                }

                return new Endpoint(3);
            });

            _ = binding.Run(e => e.Id, -1).Should().Be(-1);
            _ = binding.Run(e => e.Id, -1).Should().Be(3);
        }

        [Fact]
        public void MarkStale_rebinds_to_the_new_default_and_unbinds_the_old_one()
        {
            Endpoint first = new(1);
            Endpoint second = new(2);
            Endpoint current = first;
            List<string> log = [];
            using DefaultEndpointBinding<Endpoint> binding = new(
                () => current,
                e => log.Add($"bound {e.Id}"),
                e => log.Add($"unbound {e.Id}"));

            _ = binding.Run(e => e.Id, -1).Should().Be(1);

            current = second;
            binding.MarkStale();

            _ = binding.Run(e => e.Id, -1).Should().Be(2);
            _ = log.Should().Equal("bound 1", "unbound 1", "bound 2");
        }

        [Fact]
        public void Endpoint_that_throws_is_rebound_and_the_operation_retried_once()
        {
            // A device re-enumerated under us leaves the cached COM object dead.
            Endpoint dead = new(1);
            Endpoint live = new(2);
            Endpoint current = dead;
            using DefaultEndpointBinding<Endpoint> binding = new(() => current);

            _ = binding.Run(e => e.Id, -1).Should().Be(1);
            current = live;

            int result = binding.Run(e =>
            {
                if (e == dead)
                {
                    throw new InvalidOperationException("stale endpoint");
                }

                return e.Id;
            }, -1);

            _ = result.Should().Be(2);
        }

        [Fact]
        public void Operation_that_keeps_failing_returns_the_fallback_instead_of_throwing()
        {
            using DefaultEndpointBinding<Endpoint> binding = new(() => new Endpoint(1));

            int result = binding.Run<int>(_ => throw new InvalidOperationException("boom"), -1);

            _ = result.Should().Be(-1);
        }

        [Fact]
        public void Dispose_unbinds_the_current_endpoint()
        {
            List<string> log = [];
            DefaultEndpointBinding<Endpoint> binding = new(() => new Endpoint(1), null, e => log.Add($"unbound {e.Id}"));

            _ = binding.Run(e => e.Id, -1);
            binding.Dispose();

            _ = log.Should().Equal("unbound 1");
        }
    }
}
