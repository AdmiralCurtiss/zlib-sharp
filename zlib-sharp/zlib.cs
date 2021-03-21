// port of bits and pieces of zlib.h

namespace zlib_sharp {
	public static class zlib {
		public const string ZLIB_VERSION = "1.2.11";
		public const int ZLIB_VERNUM = 0x12b0;
		public const int ZLIB_VER_MAJOR = 1;
		public const int ZLIB_VER_MINOR = 2;
		public const int ZLIB_VER_REVISION = 11;
		public const int ZLIB_VER_SUBREVISION = 0;

		/* constants */

		public const int Z_NO_FLUSH = 0;
		public const int Z_PARTIAL_FLUSH = 1;
		public const int Z_SYNC_FLUSH = 2;
		public const int Z_FULL_FLUSH = 3;
		public const int Z_FINISH = 4;
		public const int Z_BLOCK = 5;
		public const int Z_TREES = 6;
		/* Allowed flush values; see deflate() and inflate() below for details */

		public const int Z_OK = 0;
		public const int Z_STREAM_END = 1;
		public const int Z_NEED_DICT = 2;
		public const int Z_ERRNO = (-1);
		public const int Z_STREAM_ERROR = (-2);
		public const int Z_DATA_ERROR = (-3);
		public const int Z_MEM_ERROR = (-4);
		public const int Z_BUF_ERROR = (-5);
		public const int Z_VERSION_ERROR = (-6);
		/* Return codes for the compression/decompression functions. Negative values
		 * are errors, positive values are used for special but normal events.
		 */

		public const int Z_NO_COMPRESSION = 0;
		public const int Z_BEST_SPEED = 1;
		public const int Z_BEST_COMPRESSION = 9;
		public const int Z_DEFAULT_COMPRESSION = (-1);
		/* compression levels */

		public const int Z_FILTERED = 1;
		public const int Z_HUFFMAN_ONLY = 2;
		public const int Z_RLE = 3;
		public const int Z_FIXED = 4;
		public const int Z_DEFAULT_STRATEGY = 0;
		/* compression strategy; see deflateInit2() below for details */

		public const int Z_BINARY = 0;
		public const int Z_TEXT = 1;
		public const int Z_ASCII = Z_TEXT;   /* for compatibility with 1.2.2 and earlier */
		public const int Z_UNKNOWN = 2;
		/* Possible values of the data_type field for deflate() */

		public const int Z_DEFLATED = 8;
		/* The deflate compression method (the only one supported in this version) */

		public const int Z_NULL = 0;  /* for initializing zalloc, zfree, opaque */
	}

	public class z_stream {
		// need these because we don't have pointers
		public byte[] input_buffer;
		public byte[] output_buffer;

		public long next_in;    /* next input byte */
		public uint avail_in;   /* number of bytes available at next_in */
		public ulong total_in;  /* total number of input bytes read so far */

		public long next_out;   /* next output byte will go here */
		public uint avail_out;  /* remaining free space at next_out */
		public ulong total_out; /* total number of bytes output so far */

		public string msg;      /* last error message, NULL if no error */
		public inflate_state state; /* not visible by applications */
		public deflate.deflate_state dstate;

		public int data_type;   /* best guess about the data type: binary or text
		                           for deflate, or the decoding state for inflate */
		public ulong adler;     /* Adler-32 or CRC-32 value of the uncompressed data */
		public ulong reserved;  /* reserved for future use */

		public const int _sizeof = 88;
	}

	public class gz_header {
		public int text;       /* true if compressed data believed to be text */
		public ulong time;     /* modification time */
		public int xflags;     /* extra flags (not used when writing a gzip file) */
		public int os;         /* operating system */
		public byte[] extra;   /* pointer to extra field or Z_NULL if none */
		public uint extra_len; /* extra field length (valid if extra != Z_NULL) */
		public uint extra_max; /* space at extra (only when reading header) */
		public byte[] name;    /* pointer to zero-terminated file name or Z_NULL */
		public uint name_max;  /* space at name (only when reading header) */
		public byte[] comment; /* pointer to zero-terminated comment or Z_NULL */
		public uint comm_max;  /* space at comment (only when reading header) */
		public int hcrc;       /* true if there was or will be a header crc */
		public int done;       /* true when done reading gzip header (not used
		                   when writing a gzip file) */
	}
}
