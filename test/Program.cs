using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zlib_sharp;

namespace test {
	class Program {
		static int do_compress(
			out byte[] dest_array,
			out ulong destLen,
			byte[] source_array,
			long source_index,
			ulong sourceLen,
			int level,
			int strategy
		) {
			z_stream stream = new z_stream();
			int err;
			const uint max = uint.MaxValue;
			ulong left;

			destLen = 0;
			dest_array = null;

			err = zlib.deflateInit2(stream, level, zlib.Z_DEFLATED, zlib.MAX_WBITS, zlib.MAX_MEM_LEVEL, strategy);
			if (err != zlib.Z_OK) return err;

			ulong bound = zlib.deflateBound(stream, sourceLen);
			dest_array = new byte[bound];

			left = bound;
			stream.output_buffer = dest_array;
			stream.next_out = 0;
			stream.avail_out = 0;
			stream.input_buffer = source_array;
			stream.next_in = source_index;
			stream.avail_in = 0;

			do {
				if (stream.avail_out == 0) {
					stream.avail_out = left > (ulong)max ? max : (uint)left;
					left -= stream.avail_out;
				}
				if (stream.avail_in == 0) {
					stream.avail_in = sourceLen > (ulong)max ? max : (uint)sourceLen;
					sourceLen -= stream.avail_in;
				}
				err = zlib.deflate(stream, sourceLen != 0 ? zlib.Z_NO_FLUSH : zlib.Z_FINISH);
			} while (err == zlib.Z_OK);

			destLen = stream.total_out;
			zlib.deflateEnd(stream);
			return err == zlib.Z_STREAM_END ? zlib.Z_OK : err;
		}

		static int Main(string[] args) {
			if (args.Length < 1) {
				Console.WriteLine("no file given");
				return -1;
			}

			byte[] inarr = System.IO.File.ReadAllBytes(args[0]);

			byte[] outarr;
			ulong len;

			for (int level = 0; level <= 9; ++level) {
				for (int strategy = 0; strategy <= 4; ++strategy) {
					string name = args[0] + "_lv" + level + "_strat" + strategy + "_cs_compressed.bin";
					if (do_compress(out outarr, out len, inarr, 0, (ulong)inarr.LongLength, level, strategy) == zlib.Z_OK) {
						byte[] outarr2 = new byte[len];
						for (ulong i = 0; i < len; ++i) {
							outarr2[i] = outarr[i];
						}
						System.IO.File.WriteAllBytes(name, outarr2);
						Console.WriteLine("success");
					} else {
						Console.WriteLine("fail");
						return -1;
					}
				}
			}

			return 0;
		}
	}
}
