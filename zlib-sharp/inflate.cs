// port of inflate.h and inflate.c, except for the big inflate() function

namespace zlib_sharp {
	/* inflate.h -- internal inflate state definition
	 * Copyright (C) 1995-2016 Mark Adler
	 * For conditions of distribution and use, see copyright notice in zlib.h
	 */

	/* WARNING: this file should *not* be used by applications. It is
	   part of the implementation of the compression library and is
	   subject to change. Applications should only use zlib.h.
	 */

	/* Possible inflate modes between inflate() calls */
	internal enum inflate_mode {
		HEAD = 16180,   /* i: waiting for magic header */
		FLAGS,      /* i: waiting for method and flags (gzip) */
		TIME,       /* i: waiting for modification time (gzip) */
		OS,         /* i: waiting for extra flags and operating system (gzip) */
		EXLEN,      /* i: waiting for extra length (gzip) */
		EXTRA,      /* i: waiting for extra bytes (gzip) */
		NAME,       /* i: waiting for end of file name (gzip) */
		COMMENT,    /* i: waiting for end of comment (gzip) */
		HCRC,       /* i: waiting for header crc (gzip) */
		DICTID,     /* i: waiting for dictionary check value */
		DICT,       /* waiting for inflateSetDictionary() call */

		TYPE,       /* i: waiting for type bits, including last-flag bit */
		TYPEDO,     /* i: same, but skip check to exit inflate on new block */
		STORED,     /* i: waiting for stored size (length and complement) */
		COPY_,      /* i/o: same as COPY below, but only first time in */
		COPY,       /* i/o: waiting for input or output to copy stored block */
		TABLE,      /* i: waiting for dynamic block table lengths */
		LENLENS,    /* i: waiting for code length code lengths */
		CODELENS,   /* i: waiting for length/lit and distance code lengths */

		LEN_,       /* i: same as LEN below, but only first time in */
		LEN,        /* i: waiting for length/lit/eob code */
		LENEXT,     /* i: waiting for length extra bits */
		DIST,       /* i: waiting for distance code */
		DISTEXT,    /* i: waiting for distance extra bits */
		MATCH,      /* o: waiting for output space to copy string */
		LIT,        /* o: waiting for output space to write literal */

		CHECK,      /* i: waiting for 32-bit check value */
		LENGTH,     /* i: waiting for 32-bit length (gzip) */
		DONE,       /* finished check, done -- remain here until reset */
		BAD,        /* got a data error -- remain here until reset */
		MEM,        /* got an inflate() memory error -- remain here until reset */
		SYNC        /* looking for synchronization bytes to restart inflate() */
	}

	/*
		State transitions between above modes -

		(most modes can go to BAD or MEM on error -- not shown for clarity)

		Process header:
			HEAD . (gzip) or (zlib) or (raw)
			(gzip) . FLAGS . TIME . OS . EXLEN . EXTRA . NAME . COMMENT .
					  HCRC . TYPE
			(zlib) . DICTID or TYPE
			DICTID . DICT . TYPE
			(raw) . TYPEDO
		Read deflate blocks:
				TYPE . TYPEDO . STORED or TABLE or LEN_ or CHECK
				STORED . COPY_ . COPY . TYPE
				TABLE . LENLENS . CODELENS . LEN_
				LEN_ . LEN
		Read deflate codes in fixed or dynamic block:
					LEN . LENEXT or LIT or TYPE
					LENEXT . DIST . DISTEXT . MATCH . LEN
					LIT . LEN
		Process trailer:
			CHECK . LENGTH . DONE
	 */

	/* State maintained between inflate() calls -- approximately 7K bytes, not
	   including the allocated sliding window, which is up to 32K bytes. */
	internal class inflate_state {
		public z_stream strm;             /* pointer back to this zlib stream */
		public inflate_mode mode;          /* current inflate mode */
		public int last;                   /* true if processing last block */
		public int wrap;                   /* bit 0 true for zlib, bit 1 true for gzip,
                                   bit 2 true to validate check value */
		public int havedict;               /* true if dictionary provided */
		public int flags;                  /* gzip header method and flags (0 if zlib) */
		public uint dmax;              /* zlib header max distance (INFLATE_STRICT) */
		public ulong check;        /* protected copy of check value */
		public ulong total;        /* protected copy of output count */
		public gz_header head;            /* where to save gzip header information */
		/* sliding window */
		public uint wbits;             /* log base 2 of requested window size */
		public uint wsize;             /* window size or zero if not using window */
		public uint whave;             /* valid bytes in the window */
		public uint wnext;             /* window write index */
		public byte[] window;  /* allocated sliding window, if needed */
		/* bit accumulator */
		public ulong hold;         /* input bit accumulator */
		public uint bits;              /* number of bits in "in" */
		/* for string and stored block copying */
		public uint length;            /* literal or length of data to copy */
		public uint offset;            /* distance back to copy string from */
		/* for table and code decoding */
		public uint extra;             /* extra bits needed */
		/* fixed and dynamic code tables */
		public code[] lencode_array;    /* starting table for length/literal codes */
		public long lencode_index;
		public code[] distcode_array;   /* starting table for distance codes */
		public long distcode_index;
		public uint lenbits;           /* index bits for lencode */
		public uint distbits;          /* index bits for distcode */
		/* dynamic table building */
		public uint ncode;             /* number of code length code lengths */
		public uint nlen;              /* number of length code lengths */
		public uint ndist;             /* number of distance code lengths */
		public uint have;              /* number of code lengths in lens[] */
		public long next;             /* next available space in codes[] */
		public ushort[] lens = new ushort[320];   /* temporary storage for code lengths */
		public ushort[] work = new ushort[288];   /* work area for code table building */
		public code[] codes = new code[inftrees.ENOUGH];         /* space for code tables */
		public int sane;                   /* if false, allow invalid distance too far */
		public int back;                   /* bits back of last unprocessed length/lit */
		public uint was;               /* initial length of match */
	}

	/* inflate.c -- zlib decompression
	 * Copyright (C) 1995-2016 Mark Adler
	 * For conditions of distribution and use, see copyright notice in zlib.h
	 */

	internal partial class inflate {
		private static int inflateStateCheck(
		z_stream strm) {
			inflate_state state;
			if (strm == null)
				return 1;
			state = strm.state;
			if (state == null || state.strm != strm ||
				state.mode < inflate_mode.HEAD || state.mode > inflate_mode.SYNC)
				return 1;
			return 0;
		}

		public static int inflateResetKeep(
		z_stream strm) {
			inflate_state state;

			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			strm.total_in = strm.total_out = state.total = 0;
			strm.msg = null;
			if (state.wrap != 0)        /* to support ill-conceived Java test suite */
				strm.adler = (ulong)(state.wrap & 1);
			state.mode = inflate_mode.HEAD;
			state.last = 0;
			state.havedict = 0;
			state.dmax = 32768U;
			state.head = null;
			state.hold = 0;
			state.bits = 0;
			state.next = 0;
			state.lencode_array = state.codes;
			state.distcode_array = state.codes;
			state.lencode_index = 0;
			state.distcode_index = 0;
			state.sane = 1;
			state.back = -1;
			//Tracev((stderr, "inflate: reset\n"));
			return zlib.Z_OK;
		}

		public static int inflateReset(
		z_stream strm) {
			inflate_state state;

			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			state.wsize = 0;
			state.whave = 0;
			state.wnext = 0;
			return inflateResetKeep(strm);
		}

		public static int inflateReset2(
		z_stream strm,
		int windowBits) {
			int wrap;
			inflate_state state;

			/* get the state */
			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;

			/* extract wrap request from windowBits parameter */
			if (windowBits < 0) {
				wrap = 0;
				windowBits = -windowBits;
			} else {
				wrap = (windowBits >> 4) + 5;
				if (windowBits < 48)
					windowBits &= 15;
			}

			/* set number of window bits, free window if different */
			if (windowBits != 0 && (windowBits < 8 || windowBits > 15))
				return zlib.Z_STREAM_ERROR;
			if (state.window != null && state.wbits != (uint)windowBits) {
				state.window = null;
			}

			/* update state and reset the rest of it */
			state.wrap = wrap;
			state.wbits = (uint)windowBits;
			return inflateReset(strm);
		}

		public static int inflateInit2_(
		z_stream strm,
		int windowBits,
		string version,
		int stream_size) {
			int ret;
			inflate_state state;

			if (version == null || version.Length == 0 || version[0] != zlib.ZLIB_VERSION[0] ||
				stream_size != (int)z_stream._sizeof)
				return zlib.Z_VERSION_ERROR;
			if (strm == null) return zlib.Z_STREAM_ERROR;
			strm.msg = null;                 /* in case we return an error */
			state = new inflate_state();
			if (state == null) return zlib.Z_MEM_ERROR;
			//Tracev((stderr, "inflate: allocated\n"));
			strm.state = state;
			state.strm = strm;
			state.window = null;
			state.mode = inflate_mode.HEAD;     /* to pass state test in inflateReset2() */
			ret = inflateReset2(strm, windowBits);
			if (ret != zlib.Z_OK) {
				state = null;
				strm.state = null;
			}
			return ret;
		}

		public static int inflateInit_(
		z_stream strm,
		string version,
		int stream_size) {
			const int DEF_WBITS = 15;
			return inflateInit2_(strm, DEF_WBITS, version, stream_size);
		}

		public static int inflatePrime(
		z_stream strm,
		int bits,
		int value) {
			inflate_state state;

			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			if (bits < 0) {
				state.hold = 0;
				state.bits = 0;
				return zlib.Z_OK;
			}
			if (bits > 16 || state.bits + (uint)bits > 32) return zlib.Z_STREAM_ERROR;
			value &= (int)((1L << bits) - 1);
			state.hold += (uint)value << (int)state.bits;
			state.bits += (uint)bits;
			return zlib.Z_OK;
		}

		/*
		   Return state with length and distance decoding tables and index sizes set to
		   fixed code decoding.  Normally this returns fixed tables from inffixed.h.
		   If BUILDFIXED is defined, then instead this routine builds the tables the
		   first time it's called, and returns those tables the first time and
		   thereafter.  This reduces the size of the code by about 2K bytes, in
		   exchange for a little execution time.  However, BUILDFIXED should not be
		   used for threaded applications, since the rewriting of the tables and virgin
		   may not be thread-safe.
		 */
		private static void fixedtables(
		inflate_state state) {
			state.lencode_array = inffixed.lenfix;
			state.lencode_index = 0;
			state.lenbits = 9;
			state.distcode_array = inffixed.distfix;
			state.distcode_index = 0;
			state.distbits = 5;
		}

		/*
		   Update the window with the last wsize (normally 32K) bytes written before
		   returning.  If window does not exist yet, create it.  This is only called
		   when a window is already in use, or when output has been written during this
		   inflate call, but the end of the deflate stream has not been reached yet.
		   It is also called to create a window for dictionary data when a dictionary
		   is loaded.

		   Providing output buffers larger than 32K to inflate() should provide a speed
		   advantage, since only the last 32K of output is copied to the sliding window
		   upon return from inflate(), and since all distances after the first 32K of
		   output will fall in the output data, making match copies simpler and faster.
		   The advantage may be dependent on the size of the processor's data caches.
		 */
		private static int updatewindow(
		z_stream strm,
		byte[] end_array,
		long end_index,
		uint copy) {
			inflate_state state;
			uint dist;

			state = strm.state;

			/* if it hasn't been done already, allocate space for the window */
			if (state.window == null) {
				state.window = new byte[1U << (int)state.wbits];
				if (state.window == null) return 1;
			}

			/* if window not in use yet, initialize */
			if (state.wsize == 0) {
				state.wsize = 1U << (int)state.wbits;
				state.wnext = 0;
				state.whave = 0;
			}

			/* copy state.wsize or less output bytes into the circular window */
			if (copy >= state.wsize) {
				zutil.zmemcpy(state.window, 0, end_array, end_index - state.wsize, state.wsize);
				state.wnext = 0;
				state.whave = state.wsize;
			} else {
				dist = state.wsize - state.wnext;
				if (dist > copy) dist = copy;
				zutil.zmemcpy(state.window, state.wnext, end_array, end_index - copy, dist);
				copy -= dist;
				if (copy != 0) {
					zutil.zmemcpy(state.window, 0, end_array, end_index - copy, copy);
					state.wnext = copy;
					state.whave = state.wsize;
				} else {
					state.wnext += dist;
					if (state.wnext == state.wsize) state.wnext = 0;
					if (state.whave < state.wsize) state.whave += dist;
				}
			}
			return 0;
		}

		public static int inflateEnd(
		z_stream strm) {
			inflate_state state;
			if (inflateStateCheck(strm) != 0)
				return zlib.Z_STREAM_ERROR;
			state = strm.state;
			if (state.window != null) state.window = null;
			strm.state = null;
			//Tracev((stderr, "inflate: end\n"));
			return zlib.Z_OK;
		}

		public static int inflateGetDictionary(
		z_stream strm,
		byte[] dictionary,
		ref uint dictLength) {
			inflate_state state;

			/* check state */
			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;

			/* copy dictionary */
			if (state.whave != 0 && dictionary != null) {
				zutil.zmemcpy(dictionary, 0, state.window, state.wnext,
						state.whave - state.wnext);
				zutil.zmemcpy(dictionary, state.whave - state.wnext,
						state.window, 0, state.wnext);
			}
			//if (dictLength != null)
			dictLength = state.whave;
			return zlib.Z_OK;
		}

		public static int inflateSetDictionary(
		z_stream strm,
		byte[] dictionary,
		uint dictLength) {
			inflate_state state;
			ulong dictid;
			int ret;

			/* check state */
			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			if (state.wrap != 0 && state.mode != inflate_mode.DICT)
				return zlib.Z_STREAM_ERROR;

			/* check for correct dictionary identifier */
			if (state.mode == inflate_mode.DICT) {
				dictid = adler32.adler32_(0L, null, 0, 0);
				dictid = adler32.adler32_(dictid, dictionary, 0, dictLength);
				if (dictid != state.check)
					return zlib.Z_DATA_ERROR;
			}

			/* copy dictionary to window using updatewindow(), which will amend the
			   existing dictionary if appropriate */
			ret = updatewindow(strm, dictionary, dictLength, dictLength);
			if (ret != 0) {
				state.mode = inflate_mode.MEM;
				return zlib.Z_MEM_ERROR;
			}
			state.havedict = 1;
			//Tracev((stderr, "inflate:   dictionary set\n"));
			return zlib.Z_OK;
		}

		public static int inflateGetHeader(
		z_stream strm,
		gz_header head) {
			inflate_state state;

			/* check state */
			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			if ((state.wrap & 2) == 0) return zlib.Z_STREAM_ERROR;

			/* save header structure */
			state.head = head;
			head.done = 0;
			return zlib.Z_OK;
		}

		/*
		   Search buf[0..len-1] for the pattern: 0, 0, 0xff, 0xff.  Return when found
		   or when out of input.  When called, *have is the number of pattern bytes
		   found in order so far, in 0..3.  On return *have is updated to the new
		   state.  If on return *have equals four, then the pattern was found and the
		   return value is how many bytes were read including the last byte of the
		   pattern.  If *have is less than four, then the pattern has not been found
		   yet and the return value is len.  In the latter case, syncsearch() can be
		   called again with more data and the *have state.  *have is initialized to
		   zero for the first call.
		 */
		private static uint syncsearch(
		ref uint have,
		byte[] buf_array,
		long buf_index,
		uint len) {
			uint got;
			uint next;

			got = have;
			next = 0;
			while (next < len && got < 4) {
				if ((int)(buf_array[buf_index + next]) == (got < 2 ? 0 : 0xff))
					got++;
				else if (buf_array[buf_index + next] != 0)
					got = 0;
				else
					got = 4 - got;
				next++;
			}
			have = got;
			return next;
		}

		public static int inflateSync(
		z_stream strm) {
			uint len;               /* number of bytes to look at or looked at */
			ulong @in, @out;      /* temporary to save total_in and total_out */
			byte[] buf = new byte[4];       /* to restore bit buffer to byte string */
			inflate_state state;

			/* check parameters */
			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			if (strm.avail_in == 0 && state.bits < 8) return zlib.Z_BUF_ERROR;

			/* if first time, start search in bit buffer */
			if (state.mode != inflate_mode.SYNC) {
				state.mode = inflate_mode.SYNC;
				state.hold <<= (int)(state.bits & 7);
				state.bits -= state.bits & 7;
				len = 0;
				while (state.bits >= 8) {
					buf[len++] = (byte)(state.hold);
					state.hold >>= 8;
					state.bits -= 8;
				}
				state.have = 0;
				syncsearch(ref state.have, buf, 0, len);
			}

			/* search available input */
			len = syncsearch(ref state.have, strm.input_buffer, strm.next_in, strm.avail_in);
			strm.avail_in -= len;
			strm.next_in += len;
			strm.total_in += len;

			/* return no joy or set up to restart inflate() on a new block */
			if (state.have != 4) return zlib.Z_DATA_ERROR;
			@in = strm.total_in; @out = strm.total_out;
			inflateReset(strm);
			strm.total_in = @in; strm.total_out = @out;
			state.mode = inflate_mode.TYPE;
			return zlib.Z_OK;
		}

		/*
		   Returns true if inflate is currently at the end of a block generated by
		   Z_SYNC_FLUSH or Z_FULL_FLUSH. This function is used by one PPP
		   implementation to provide an additional safety check. PPP uses
		   Z_SYNC_FLUSH but removes the length bytes of the resulting empty stored
		   block. When decompressing, PPP checks that at the end of input packet,
		   inflate is waiting for these length bytes.
		 */
		public static int inflateSyncPoint(
		z_stream strm) {
			inflate_state state;

			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			return (state.mode == inflate_mode.STORED && state.bits == 0) ? 1 : 0;
		}

		public static int inflateCopy(
		z_stream dest,
		z_stream source) {
			inflate_state state;
			inflate_state copy;
			byte[] window;
			uint wsize;

			/* check input */
			if (inflateStateCheck(source) != 0 || dest == null)
				return zlib.Z_STREAM_ERROR;
			state = source.state;

			/* allocate space */
			copy = new inflate_state();
			if (copy == null) return zlib.Z_MEM_ERROR;
			window = null;
			if (state.window != null) {
				window = new byte[1U << (int)state.wbits];
				if (window == null) {
					copy = null;
					return zlib.Z_MEM_ERROR;
				}
			}

			/* copy state */
			zutil.zmemcpy(dest, source);
			zutil.zmemcpy(copy, state);
			copy.strm = dest;
			if (state.lencode_array == state.codes) {
				copy.lencode_array = copy.codes;
				copy.lencode_index = state.lencode_index;
				copy.distcode_array = copy.codes;
				copy.distcode_index = state.distcode_index;
			}
			copy.next = state.next;
			if (window != null) {
				wsize = 1U << (int)state.wbits;
				zutil.zmemcpy(window, 0, state.window, 0, wsize);
			}
			copy.window = window;
			dest.state = copy;
			return zlib.Z_OK;
		}

		public static int inflateUndermine(
		z_stream strm,
		int subvert) {
			inflate_state state;

			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			//#ifdef INFLATE_ALLOW_INVALID_DISTANCE_TOOFAR_ARRR
			//    state.sane = !subvert;
			//    return zlib.Z_OK;
			//#else
			state.sane = 1;
			return zlib.Z_DATA_ERROR;
			//#endif
		}

		public static int inflateValidate(
		z_stream strm,
		int check) {
			inflate_state state;

			if (inflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;
			state = strm.state;
			if (check != 0)
				state.wrap |= 4;
			else
				state.wrap &= ~4;
			return zlib.Z_OK;
		}

		public static long inflateMark(
		z_stream strm) {
			inflate_state state;

			if (inflateStateCheck(strm) != 0)
				return -(1L << 16);
			state = strm.state;
			return (long)(((ulong)((long)state.back)) << 16) +
				(state.mode == inflate_mode.COPY ? state.length :
					(state.mode == inflate_mode.MATCH ? state.was - state.length : 0));
		}

		public static ulong inflateCodesUsed(
		z_stream strm) {
			inflate_state state;
			if (inflateStateCheck(strm) != 0) return ulong.MaxValue;
			state = strm.state;
			return (ulong)(state.next);
		}
	}
}
