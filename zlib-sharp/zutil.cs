// port of bits and pieces of zutil.h

namespace zlib_sharp {
	public static class zutil {
		public const int DEF_MEM_LEVEL = 8;
		/* default memLevel */

		public const int STORED_BLOCK = 0;
		public const int STATIC_TREES = 1;
		public const int DYN_TREES = 2;
		/* The three kinds of block type */

		public const int MIN_MATCH = 3;
		public const int MAX_MATCH = 258;
		/* The minimum and maximum match lengths */

		public const int PRESET_DICT = 0x20; /* preset dictionary flag in zlib header */

		public const byte OS_CODE = 0;

		internal static void zmemcpy(byte[] target, long target_index, byte[] source, long source_index, long count) {
			for (long i = 0; i < count; ++i) {
				target[target_index + i] = source[source_index + i];
			}
		}

		internal static void zmemcpy(z_stream target, z_stream source) {
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
			target.dstate = source.dstate;

			target.data_type = source.data_type;

			target.adler = source.adler;
			target.reserved = source.reserved;
		}

		internal static void zmemcpy(inflate_state target, inflate_state source) {
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

		internal static uint ZSWAP32(uint q) {
			return ((((q) >> 24) & 0xff) + (((q) >> 8) & 0xff00) +
					(((q) & 0xff00) << 8) + (((q) & 0xff) << 24));
		}

		private static string[] z_errmsg = new string[10] {
			"need dictionary",     /* Z_NEED_DICT       2  */
			"stream end",          /* Z_STREAM_END      1  */
			"",                    /* Z_OK              0  */
			"file error",          /* Z_ERRNO         (-1) */
			"stream error",        /* Z_STREAM_ERROR  (-2) */
			"data error",          /* Z_DATA_ERROR    (-3) */
			"insufficient memory", /* Z_MEM_ERROR     (-4) */
			"buffer error",        /* Z_BUF_ERROR     (-5) */
			"incompatible version",/* Z_VERSION_ERROR (-6) */
			""
		};

		public static string ERR_MSG(int err) {
			return z_errmsg[zlib.Z_NEED_DICT - (err)];
		}

		internal static void zmemzero(byte[] target, long target_index, uint count) {
			for (uint i = 0; i < count; ++i) {
				target[target_index + i] = 0;
			}
		}
	}
}
