// port of uncompr.c

namespace zlib_sharp {
	internal static class uncompr {
		/* uncompr.c -- decompress a memory buffer
		 * Copyright (C) 1995-2003, 2010, 2014, 2016 Jean-loup Gailly, Mark Adler
		 * For conditions of distribution and use, see copyright notice in zlib.h
		 */

		/* ===========================================================================
		     Decompresses the source buffer into the destination buffer.  *sourceLen is
		   the byte length of the source buffer. Upon entry, *destLen is the total size
		   of the destination buffer, which must be large enough to hold the entire
		   uncompressed data. (The size of the uncompressed data must have been saved
		   previously by the compressor and transmitted to the decompressor by some
		   mechanism outside the scope of this compression library.) Upon exit,
		   *destLen is the size of the decompressed data and *sourceLen is the number
		   of source bytes consumed. Upon return, source + *sourceLen points to the
		   first unused input byte.

		     uncompress returns Z_OK if success, Z_MEM_ERROR if there was not enough
		   memory, Z_BUF_ERROR if there was not enough room in the output buffer, or
		   Z_DATA_ERROR if the input data was corrupted, including if the input data is
		   an incomplete zlib stream.
		*/
		public static int uncompress2(byte[] dest_array, long dest_index, ref ulong destLen, byte[] source_array, long source_index, ref ulong sourceLen) {
			z_stream stream = new z_stream();
			int err;
			const uint max = uint.MaxValue;
			ulong len, left;
			byte[] buf = new byte[1];    /* for detection of incomplete stream when *destLen == 0 */

			len = sourceLen;
			if (destLen != 0) {
				left = destLen;
				destLen = 0;
			} else {
				left = 1;
				dest_array = buf;
				dest_index = 0;
			}

			stream.input_buffer = source_array;
			stream.next_in = source_index;
			stream.avail_in = 0;
			//stream.zalloc = (alloc_func)0;
			//stream.zfree = (free_func)0;
			//stream.opaque = (voidpf)0;

			err = zlib_sharp.inflate.inflateInit_(stream, zlib.ZLIB_VERSION, z_stream._sizeof);
			if (err != zlib_sharp.zlib.Z_OK) return err;

			stream.output_buffer = dest_array;
			stream.next_out = dest_index;
			stream.avail_out = 0;

			do {
				if (stream.avail_out == 0) {
					stream.avail_out = left > (ulong)max ? max : (uint)left;
					left -= stream.avail_out;
				}
				if (stream.avail_in == 0) {
					stream.avail_in = len > (ulong)max ? max : (uint)len;
					len -= stream.avail_in;
				}
				err = zlib_sharp.inflate.inflate_(stream, zlib_sharp.zlib.Z_NO_FLUSH);
			} while (err == zlib_sharp.zlib.Z_OK);

			sourceLen -= len + stream.avail_in;
			if (dest_array != buf)
				destLen = stream.total_out;
			else if (stream.total_out != 0 && err == zlib_sharp.zlib.Z_BUF_ERROR)
				left = 1;

			zlib_sharp.inflate.inflateEnd(stream);
			return err == zlib_sharp.zlib.Z_STREAM_END ? zlib_sharp.zlib.Z_OK :
				   err == zlib_sharp.zlib.Z_NEED_DICT ? zlib_sharp.zlib.Z_DATA_ERROR :
				   err == zlib_sharp.zlib.Z_BUF_ERROR && (left + stream.avail_out) != 0 ? zlib_sharp.zlib.Z_DATA_ERROR :
				   err;
		}

		public static int uncompress(byte[] dest_array, long dest_index, ref ulong destLen, byte[] source_array, long source_index, ulong sourceLen) {
			return uncompress2(dest_array, dest_index, ref destLen, source_array, source_index, ref sourceLen);
		}
	}
}
