using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core2;
using GeminiLab.Core2.Collections;
using GeminiLab.Core2.Random;
using GeminiLab.Core2.Random.RNG;
using GeminiLab.Core2.Yielder;

namespace Pranove {
    public class RanCore : IRNG<ulong>, IDisposable {
        private          bool        _disposed;
        private readonly IRNG<ulong> _rng;

        private readonly Thread      _kicker;
        private readonly IRNG<ulong> _kickerRng;

        private void Kicker() {
            ulong span = 0x13579bdfeca86420;

            unchecked {
                while (!_disposed) {
                    lock (_rng) {
                        span = (span * _rng.Next() + DefaultRNG.NextU64()) ^ _kickerRng.Next();
                        span = (span << 57) | (span >> 7);
                    }

                    Thread.Sleep((int) (span & 0xff));
                }
            }
        }

        public RanCore() {
            _rng = new I32ToU64RNG(new PCG(DefaultRNG.NextU64(), DefaultRNG.NextU64()));
            _kickerRng = new I32ToU64RNG(new PCG(_rng.Next() ^ DefaultRNG.NextU64()));
            _kicker = new Thread(Kicker);
        }

        public void Start() {
            _kicker.Start();
        }

        public ulong Next() {
            return _rng.Next();
        }

        public void Dispose() {
            _disposed = true;
            _kicker.Join();
        }
    }

    public static class Entry {
        public static int Main() {
            using var rc = new RanCore();

            rc.Start();

            0.To(1024).ForEach(i => {
                Thread.Sleep(100);
                // ReSharper disable once AccessToDisposedClosure
                Console.WriteLine("{0:0000}: {1:X16}", i, rc.Next());
            });

            return 0;
        }
    }
}
