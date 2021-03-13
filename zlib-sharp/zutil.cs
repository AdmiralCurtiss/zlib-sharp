// port of bits and pieces of zutil.h

namespace zlib_sharp {
	internal static class zutil {
		public static void zmemcpy(byte[] target, long target_index, byte[] source, long source_index, long count) {
			for (long i = 0; i < count; ++i) {
				target[target_index + i] = source[source_index + i];
			}
		}

		public static void zmemcpy(z_stream target, z_stream source) {
			target.input_buffer = source.input_buffer;
			target.output_buffer = source.output_buffer;

			target.next_in = source.next_in;
			target.avail_in = source.avail_in;
			target.total_in = source.total_in;

			target.next_out = source.next_out;
			target.avail_out = source.avail_out;
			target.total_out = source.total_out;

			target.msg = source.msg;
			target.state = source.state;

			target.data_type = source.data_type;

			target.adler = source.adler;
			target.reserved = source.reserved;
		}

		public static void zmemcpy(inflate_state target, inflate_state source) {
			target.strm = source.strm;
			target.mode = source.mode;
			target.last = source.last;
			target.wrap = source.wrap;
			target.havedict = source.havedict;
			target.flags = source.flags;
			target.dmax = source.dmax;
			target.check = source.check;
			target.total = source.total;
			target.head = source.head;
			target.wbits = source.wbits;
			target.wsize = source.wsize;
			target.whave = source.whave;
			target.wnext = source.wnext;
			target.window = source.window;
			target.hold = source.hold;
			target.bits = source.bits;
			target.length = source.length;
			target.offset = source.offset;
			target.extra = source.extra;
			target.lencode_array = source.lencode_array;
			target.lencode_index = source.lencode_index;
			target.distcode_array = source.distcode_array;
			target.distcode_index = source.distcode_index;
			target.lenbits = source.lenbits;
			target.distbits = source.distbits;
			target.ncode = source.ncode;
			target.nlen = source.nlen;
			target.ndist = source.ndist;
			target.have = source.have;
			target.next = source.next;
			target.lens = new ushort[320];
			for (int i = 0; i < 320; ++i) {
				target.lens[i] = source.lens[i];
			}
			target.work = new ushort[288];
			for (int i = 0; i < 288; ++i) {
				target.work[i] = source.work[i];
			}
			target.codes = new code[inftrees.ENOUGH];
			for (int i = 0; i < inftrees.ENOUGH; ++i) {
				target.codes[i] = source.codes[i];
			}
			target.sane = source.sane;
			target.back = source.back;
			target.was = source.was;
		}

		public static uint ZSWAP32(uint q) {
			return ((((q) >> 24) & 0xff) + (((q) >> 8) & 0xff00) +
					(((q) & 0xff00) << 8) + (((q) & 0xff) << 24));
		}
	}
}
