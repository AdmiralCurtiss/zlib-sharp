// port of compress.c

namespace zlib_sharp {
    internal static class compress {
/* compress.c -- compress a memory buffer
 * Copyright (C) 1995-2005, 2014, 2016 Jean-loup Gailly, Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/* ===========================================================================
     Compresses the source buffer into the destination buffer. The level
   parameter has the same meaning as in deflateInit.  sourceLen is the byte
   length of the source buffer. Upon entry, destLen is the total size of the
   destination buffer, which must be at least 0.1% larger than sourceLen plus
   12 bytes. Upon exit, destLen is the actual size of the compressed buffer.

     compress2 returns Z_OK if success, Z_MEM_ERROR if there was not enough
   memory, Z_BUF_ERROR if there was not enough room in the output buffer,
   Z_STREAM_ERROR if the level parameter is invalid.
*/
public static int compress2 (
    byte[] dest_array,
    long dest_index,
    ref ulong destLen,
    byte[] source_array,
    long source_index,
    ulong sourceLen,
    int level)
{
    z_stream stream = new z_stream();
    int err;
    const uint max = uint.MaxValue;
    ulong left;

    left = destLen;
    destLen = 0;

    err = deflate.deflateInit_(stream, level, zlib.ZLIB_VERSION, z_stream._sizeof);
    if (err != zlib.Z_OK) return err;

    stream.output_buffer = dest_array;
    stream.next_out = dest_index;
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
        err = deflate.deflate_(stream, sourceLen != 0 ? zlib.Z_NO_FLUSH : zlib.Z_FINISH);
    } while (err == zlib.Z_OK);

    destLen = stream.total_out;
    deflate.deflateEnd(stream);
    return err == zlib.Z_STREAM_END ? zlib.Z_OK : err;
}

/* ===========================================================================
 */
public static int compress_(
    byte[] dest_array,
    long dest_index,
    ref ulong destLen,
    byte[] source_array,
    long source_index,
    ulong sourceLen)
{
    return compress2(dest_array, dest_index, ref destLen, source_array, source_index, sourceLen, zlib.Z_DEFAULT_COMPRESSION);
}

/* ===========================================================================
     If the default memLevel or windowBits for deflateInit() is changed, then
   this function needs to be updated.
 */
public static ulong compressBound(ulong sourceLen) {
    return sourceLen + (sourceLen >> 12) + (sourceLen >> 14) +
           (sourceLen >> 25) + 13;
}
	}
}
