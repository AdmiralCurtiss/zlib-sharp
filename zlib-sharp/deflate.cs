// port of deflate.h and deflate.c

namespace zlib_sharp {
    internal static class deflate {
/* deflate.h -- internal compression state
 * Copyright (C) 1995-2016 Jean-loup Gailly
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/* WARNING: this file should *not* be used by applications. It is
   part of the implementation of the compression library and is
   subject to change. Applications should only use zlib.h.
 */

/* ===========================================================================
 * Internal compression state.
 */

public const int LENGTH_CODES = 29;
/* number of length codes, not counting the special END_BLOCK code */

public const int LITERALS = 256;
/* number of literal bytes 0..255 */

public const int L_CODES = (LITERALS+1+LENGTH_CODES);
/* number of Literal or Length codes, including the END_BLOCK code */

public const int D_CODES = 30;
/* number of distance codes */

public const int BL_CODES = 19;
/* number of codes used to transfer the bit lengths */

public const int HEAP_SIZE = (2*L_CODES+1);
/* maximum heap size */

public const int MAX_BITS = 15;
/* All codes must not exceed MAX_BITS bits */

public const int Buf_size = 16;
/* size of bit buffer in bi_buf */

public const int INIT_STATE = 42;    /* zlib header -> BUSY_STATE */
//#ifdef GZIP
public const int GZIP_STATE = 57;    /* gzip header -> BUSY_STATE | EXTRA_STATE */
//#endif
public const int EXTRA_STATE   = 69;     /* gzip extra block -> NAME_STATE */
public const int NAME_STATE    = 73;     /* gzip file name -> COMMENT_STATE */
public const int COMMENT_STATE = 91;     /* gzip comment -> HCRC_STATE */
public const int HCRC_STATE    = 103;    /* gzip header CRC -> BUSY_STATE */
public const int BUSY_STATE    = 113;    /* deflate -> FINISH_STATE */
public const int FINISH_STATE  = 666;    /* stream complete */
/* Stream status */


/* Data structure describing a single value and its code string. */
internal struct ct_data {
    //union {
    //    ush  freq;       /* frequency count */
    //    ush  code;       /* bit string */
    //} fc;
    //union {
    //    ush  dad;        /* father node in Huffman tree */
    //    ush  len;        /* length of bit string */
    //} dl;
    public ushort freq_code;
    public ushort dad_len;

    public ct_data(ushort freq_code, ushort dad_len) {
        this.freq_code = freq_code;
        this.dad_len = dad_len;
    }
}

//#define Freq fc.freq
//#define Code fc.code
//#define Dad  dl.dad
//#define Len  dl.len

internal struct tree_desc {
    public ct_data[] dyn_tree;           /* the dynamic tree */
    public int     max_code;            /* largest code with non zero frequency */
    public trees.static_tree_desc stat_desc;  /* the corresponding static tree */
}

//typedef ush Pos;
//typedef Pos FAR Posf;
//typedef unsigned IPos;

internal struct ushort_array_from_byte_array {
    public byte[] data;
    public long offset;

    public ushort_array_from_byte_array(byte[] data, long offset) {
        this.data = data;
        this.offset = offset;
    }

    public ushort this[long i] {
        get {
            byte a = data[offset + i * 2];
            byte b = data[offset + i * 2 + 1];
            return (ushort)((b << 8) | a);
        }
        set {
            byte a = (byte)(value & 0xff);
            byte b = (byte)((value >> 8) & 0xff);
            data[offset + i * 2] = a;
            data[offset + i * 2 + 1] = b;
        }
    }
}

/* A Pos is an index in the character window. We use short instead of int to
 * save space in the various tables. IPos is used only for parameter passing.
 */

internal class deflate_state {
    public z_stream strm;      /* pointer back to this zlib stream */
    public int   status;        /* as the name implies */
    public byte[] pending_buf;  /* output still pending */
    public ulong pending_buf_size; /* size of pending_buf */
    public byte[] pending_out_array;  /* next pending byte to output to the stream */
    public long pending_out_index;
    public ulong   pending;       /* nb of bytes in the pending buffer */
    public int   wrap;          /* bit 0 true for zlib, bit 1 true for gzip */
    public gz_header  gzhead;  /* gzip header information to write */
    public ulong   gzindex;       /* where in extra, name, or comment */
    public byte  method;        /* can only be DEFLATED */
    public int   last_flush;    /* value of flush param for previous deflate call */

                /* used by deflate.c: */

    public uint  w_size;        /* LZ77 window size (32K by default) */
    public uint  w_bits;        /* log2(w_size)  (8..16) */
    public uint  w_mask;        /* w_size - 1 */

    public byte[] window_array;
    public long window_index;
    /* Sliding window. Input bytes are read into the second half of the window,
     * and move to the first half later to keep a dictionary of at least wSize
     * bytes. With this organization, matches are limited to a distance of
     * wSize-MAX_MATCH bytes, but this ensures that IO is always
     * performed with a length multiple of the block size. Also, it limits
     * the window size to 64K, which is quite useful on MSDOS.
     * To do: use the user input buffer as sliding window.
     */

    public ulong window_size;
    /* Actual size of window: 2*wSize, except when the user input buffer
     * is directly used as sliding window.
     */

    public ushort[] prev_array;
    public long prev_index;
    /* Link to older string with same hash index. To limit the size of this
     * array to 64K, this link is maintained only for the last 32K strings.
     * An index in this array is thus a window index modulo 32K.
     */

    public ushort[] head_array; /* Heads of the hash chains or NIL. */
    public long head_index;

    public uint  ins_h;          /* hash index of string to be inserted */
    public uint  hash_size;      /* number of elements in hash table */
    public uint  hash_bits;      /* log2(hash_size) */
    public uint  hash_mask;      /* hash_size-1 */

    public int  hash_shift;
    /* Number of bits by which ins_h must be shifted at each input
     * step. It must be such that after MIN_MATCH steps, the oldest
     * byte no longer takes part in the hash key, that is:
     *   hash_shift * MIN_MATCH >= hash_bits
     */

    public long block_start;
    /* Window position at the beginning of the current output block. Gets
     * negative when the window is moved backwards.
     */

    public uint match_length;           /* length of best match */
    public uint prev_match;             /* previous match */
    public int match_available;         /* set if previous match exists */
    public uint strstart;               /* start of string to insert */
    public uint match_start;            /* start of matching string */
    public uint lookahead;              /* number of valid bytes ahead in window */

    public uint prev_length;
    /* Length of the best match at previous step. Matches not greater than this
     * are discarded. This is used in the lazy match evaluation.
     */

    public uint max_chain_length;
    /* To speed up deflation, hash chains are never searched beyond this
     * length.  A higher limit improves compression ratio but degrades the
     * speed.
     */

    public uint max_lazy_match;
    /* Attempt to find a better match only when the current match is strictly
     * smaller than this value. This mechanism is used only for compression
     * levels >= 4.
     */
//#   define max_insert_length  max_lazy_match
    /* Insert new strings in the hash table only if the match length is not
     * greater than this length. This saves time but degrades compression.
     * max_insert_length is used only for compression levels <= 3.
     */

    public int level;    /* compression level (1..9) */
    public int strategy; /* favor or force Huffman coding*/

    public uint good_match;
    /* Use a faster search when the previous match is longer than this */

    public int nice_match; /* Stop searching when current match exceeds this */

                /* used by trees.c: */
    /* Didn't use ct_data typedef below to suppress compiler warning */
    public ct_data[] dyn_ltree = new ct_data[HEAP_SIZE];   /* literal and length tree */
    public ct_data[] dyn_dtree = new ct_data[2*D_CODES+1]; /* distance tree */
    public ct_data[] bl_tree = new ct_data[2*BL_CODES+1];  /* Huffman tree for bit lengths */

    public tree_desc l_desc;               /* desc. for literal tree */
    public tree_desc d_desc;               /* desc. for distance tree */
    public tree_desc bl_desc;              /* desc. for bit length tree */

    public ushort[] bl_count = new ushort[MAX_BITS+1];
    /* number of codes at each bit length for an optimal tree */

    public int[] heap = new int[2*L_CODES+1];      /* heap used to build the Huffman trees */
    public int heap_len;               /* number of elements in the heap */
    public int heap_max;               /* element of largest frequency */
    /* The sons of heap[n] are heap[2*n] and heap[2*n+1]. heap[0] is not used.
     * The same heap array is used to build all trees.
     */

    public byte[] depth = new byte[2*L_CODES+1];
    /* Depth of each subtree used as tie breaker for trees of equal frequency
     */

    public byte[] l_buf_array;          /* buffer for literals or lengths */
    public long l_buf_index;

    public uint  lit_bufsize;
    /* Size of match buffer for literals/lengths.  There are 4 reasons for
     * limiting lit_bufsize to 64K:
     *   - frequencies can be kept in 16 bit counters
     *   - if compression is not successful for the first block, all input
     *     data is still in the window so we can still emit a stored block even
     *     when input comes from standard input.  (This can also be done for
     *     all blocks if lit_bufsize is not greater than 32K.)
     *   - if compression is not successful for a file smaller than 64K, we can
     *     even emit a stored file instead of a stored block (saving 5 bytes).
     *     This is applicable only for zip (not gzip or zlib).
     *   - creating new Huffman trees less frequently may not provide fast
     *     adaptation to changes in the input data statistics. (Take for
     *     example a binary file with poorly compressible code followed by
     *     a highly compressible string table.) Smaller buffer sizes give
     *     fast adaptation but have of course the overhead of transmitting
     *     trees more frequently.
     *   - I can't count above 4
     */

    public uint last_lit;      /* running index in l_buf */

    public ushort_array_from_byte_array d_buf;
    /* Buffer for distances. To simplify the code, d_buf and l_buf have
     * the same number of elements. To use different lengths, an extra flag
     * array would be necessary.
     */

    public ulong opt_len;        /* bit length of current block with optimal trees */
    public ulong static_len;     /* bit length of current block with static trees */
    public uint matches;       /* number of string matches in current block */
    public uint insert;        /* bytes at end of window left to insert */

//#ifdef ZLIB_DEBUG
//    ulong compressed_len; /* total bit length of compressed file mod 2^32 */
//    ulong bits_sent;      /* bit length of compressed data sent mod 2^32 */
//#endif

    public ushort bi_buf;
    /* Output buffer. bits are inserted starting at the bottom (least
     * significant bits).
     */
    public int bi_valid;
    /* Number of valid bits in bi_buf.  All bits above the last valid bit
     * are always zero.
     */

    public ulong high_water;
    /* High water mark offset in window for initialized bytes -- bytes above
     * this are set to zero in order to avoid memory check warnings when
     * longest match routines access bytes past the input.  This is then
     * updated to the new high water mark.
     */
}

/* Output a byte on the stream.
 * IN assertion: there is enough room in pending_buf.
 */
public static void put_byte(deflate_state s, byte c) {
    s.pending_buf[s.pending++] = c;
}

public const int MIN_LOOKAHEAD = (zutil.MAX_MATCH+zutil.MIN_MATCH+1);
/* Minimum amount of lookahead, except at the end of the input file.
 * See deflate.c for comments about the MIN_MATCH+1.
 */

private static uint MAX_DIST(deflate_state s) { return (s.w_size-((uint)MIN_LOOKAHEAD)); }
/* In order to simplify the code, particularly on 16 bit machines, match
 * distances are limited to MAX_DIST instead of WSIZE.
 */

public const int WIN_INIT = zutil.MAX_MATCH;
/* Number of bytes after end of data in window to initialize in order to avoid
   memory checker errors from longest match routines */

internal static byte d_code(long dist) {
   return ((dist) < 256 ? trees._dist_code[dist] : trees._dist_code[256+((dist)>>7)]);
}
/* Mapping from a distance to a distance code. dist is the distance - 1 and
 * must not have side effects. _dist_code[256] and _dist_code[257] are never
 * used.
 */

//#ifndef ZLIB_DEBUG
///* Inline versions of _tr_tally for speed: */
//
//#if defined(GEN_TREES_H) || !defined(STDC)
//  extern uch ZLIB_INTERNAL _length_code[];
//  extern uch ZLIB_INTERNAL _dist_code[];
//#else
//  extern const uch ZLIB_INTERNAL _length_code[];
//  extern const uch ZLIB_INTERNAL _dist_code[];
//#endif
//
private static void _tr_tally_lit(deflate_state s, byte c, out int flush)
  { byte cc = (c);
    s.d_buf[s.last_lit] = 0;
    s.l_buf_array[s.l_buf_index + s.last_lit++] = cc;
    s.dyn_ltree[cc].freq_code++;
    flush = (s.last_lit == s.lit_bufsize-1) ? 1 : 0;
   }
private static void _tr_tally_dist(deflate_state s, uint distance, uint length, out int flush)
  { byte len = (byte)(length);
    ushort dist = (ushort)(distance);
    s.d_buf[s.last_lit] = dist;
    s.l_buf_array[s.l_buf_index + s.last_lit++] = len;
    dist--;
    s.dyn_ltree[trees._length_code[len]+LITERALS+1].freq_code++;
    s.dyn_dtree[d_code(dist)].freq_code++;
    flush = (s.last_lit == s.lit_bufsize-1) ? 1 : 0;
  }
//#else
//# define _tr_tally_lit(s, c, flush) flush = _tr_tally(s, 0, c)
//# define _tr_tally_dist(s, distance, length, flush) \
//              flush = _tr_tally(s, distance, length)
//#endif
//
//#endif /* DEFLATE_H */




/* deflate.c -- compress data using the deflation algorithm
 * Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/*
 *  ALGORITHM
 *
 *      The "deflation" process depends on being able to identify portions
 *      of the input text which are identical to earlier input (within a
 *      sliding window trailing behind the input currently being processed).
 *
 *      The most straightforward technique turns out to be the fastest for
 *      most input files: try all possible matches and select the longest.
 *      The key feature of this algorithm is that insertions into the string
 *      dictionary are very simple and thus fast, and deletions are avoided
 *      completely. Insertions are performed at each input character, whereas
 *      string matches are performed only when the previous match ends. So it
 *      is preferable to spend more time in matches to allow very fast string
 *      insertions and avoid deletions. The matching algorithm for small
 *      strings is inspired from that of Rabin & Karp. A brute force approach
 *      is used to find longer strings when a small match has been found.
 *      A similar algorithm is used in comic (by Jan-Mark Wams) and freeze
 *      (by Leonid Broukhis).
 *         A previous version of this file used a more sophisticated algorithm
 *      (by Fiala and Greene) which is guaranteed to run in linear amortized
 *      time, but has a larger average cost, uses more memory and is patented.
 *      However the F&G algorithm may be faster for some highly redundant
 *      files if the parameter max_chain_length (described below) is too large.
 *
 *  ACKNOWLEDGEMENTS
 *
 *      The idea of lazy evaluation of matches is due to Jan-Mark Wams, and
 *      I found it in 'freeze' written by Leonid Broukhis.
 *      Thanks to many people for bug reports and testing.
 *
 *  REFERENCES
 *
 *      Deutsch, L.P.,"DEFLATE Compressed Data Format Specification".
 *      Available in http://tools.ietf.org/html/rfc1951
 *
 *      A description of the Rabin and Karp algorithm is given in the book
 *         "Algorithms" by R. Sedgewick, Addison-Wesley, p252.
 *
 *      Fiala,E.R., and Greene,D.H.
 *         Data Compression with Finite Windows, Comm.ACM, 32,4 (1989) 490-595
 *
 */

public const string deflate_copyright =
   " deflate 1.2.11 Copyright 1995-2017 Jean-loup Gailly and Mark Adler ";
/*
  If you use the zlib library in a product, an acknowledgment is welcome
  in the documentation of your product. If for some reason you cannot
  include such an acknowledgment, I would appreciate that you keep this
  copyright string in the executable of your product.
 */

/* ===========================================================================
 *  Function prototypes.
 */
public enum block_state {
    need_more,      /* block not completed, need more input or more output */
    block_done,     /* block flush performed */
    finish_started, /* finish started, need only more output at next deflate */
    finish_done,    /* finish done, accept no more input or output */

    _continue
}

public delegate block_state compress_func(deflate_state s, int flush);
/* Compression function. Returns the block state after the call. */

//#ifdef ZLIB_DEBUG
//local  void check_match OF((deflate_state *s, IPos start, IPos match,
//                            int length));
//#endif

/* ===========================================================================
 * Local data
 */

public const int NIL = 0;
/* Tail of hash chains */

public const int TOO_FAR = 4096;
/* Matches of length 3 are discarded if their distance exceeds TOO_FAR */

/* Values for max_lazy_match, good_match and max_chain_length, depending on
 * the desired pack level (0..9). The values given below have been tuned to
 * exclude worst case performance for pathological files. Better values may be
 * found for specific files.
 */
internal struct config {
    public ushort good_length; /* reduce lazy search above this match length */
    public ushort max_lazy;    /* do not perform lazy search above this match length */
    public ushort nice_length; /* quit search above this match length */
    public ushort max_chain;
    public compress_func func;
    
    public config(ushort good_length, ushort max_lazy, ushort nice_length, ushort max_chain, compress_func func) { 
        this.good_length = good_length;
        this.max_lazy = max_lazy;
        this.nice_length = nice_length;
        this.max_chain = max_chain;
        this.func = func;
    }
}

//#ifdef FASTEST
//local const config configuration_table[2] = {
///*      good lazy nice chain */
///* 0 */ {0,    0,  0,    0, deflate_stored},  /* store only */
///* 1 */ {4,    4,  8,    4, deflate_fast}}; /* max speed, no lazy matches */
//#else
private static config[] configuration_table = new config[10] {
/*      good lazy nice chain */
/* 0 */ new config(0,    0,  0,    0, deflate_stored),  /* store only */
/* 1 */ new config(4,    4,  8,    4, deflate_fast), /* max speed, no lazy matches */
/* 2 */ new config(4,    5, 16,    8, deflate_fast),
/* 3 */ new config(4,    6, 32,   32, deflate_fast),

/* 4 */ new config(4,    4, 16,   16, deflate_slow),  /* lazy matches */
/* 5 */ new config(8,   16, 32,   32, deflate_slow),
/* 6 */ new config(8,   16, 128, 128, deflate_slow),
/* 7 */ new config(8,   32, 128, 256, deflate_slow),
/* 8 */ new config(32, 128, 258, 1024, deflate_slow),
/* 9 */ new config(32, 258, 258, 4096, deflate_slow)}; /* max compression */
//#endif

/* Note: the deflate() code requires max_lazy >= MIN_MATCH and max_chain >= 4
 * For deflate_fast() (levels <= 3) good is ignored and lazy has a different
 * meaning.
 */

/* rank Z_BLOCK between Z_NO_FLUSH and Z_PARTIAL_FLUSH */
private static int RANK(int f) {
    return (((f) * 2) - ((f) > 4 ? 9 : 0));
}

/* ===========================================================================
 * Update a hash value with the given input byte
 * IN  assertion: all calls to UPDATE_HASH are made with consecutive input
 *    characters, so that a running hash key can be computed from the previous
 *    key instead of complete recalculation each time.
 */
private static void UPDATE_HASH(deflate_state s, ref uint h, byte c) {
    h = (((h)<<s.hash_shift) ^ (c)) & s.hash_mask;
}


/* ===========================================================================
 * Insert string str in the dictionary and set match_head to the previous head
 * of the hash chain (the most recent string with same hash key). Return
 * the previous length of the hash chain.
 * If this file is compiled with -DFASTEST, the compression level is forced
 * to 1, and no hash chains are maintained.
 * IN  assertion: all calls to INSERT_STRING are made with consecutive input
 *    characters and the first MIN_MATCH bytes of str are valid (except for
 *    the last MIN_MATCH-1 bytes of the input file).
 */
//#ifdef FASTEST
//#define INSERT_STRING(s, str, match_head) \
//   (UPDATE_HASH(s, s->ins_h, s->window[(str) + (MIN_MATCH-1)]), \
//    match_head = s->head[s->ins_h], \
//    s->head[s->ins_h] = (Pos)(str))
//#else
private static void INSERT_STRING(deflate_state s, uint str, ref uint match_head) {
    UPDATE_HASH(s, ref s.ins_h, s.window_array[s.window_index + (str) + (zutil.MIN_MATCH-1)]);
    match_head = s.prev_array[s.prev_index + ((str) & s.w_mask)] = s.head_array[s.head_index + s.ins_h];
    s.head_array[s.head_index + s.ins_h] = (ushort)(str);
}
//#endif

/* ===========================================================================
 * Initialize the hash table (avoiding 64K overflow for 16 bit systems).
 * prev[] will be initialized on the fly.
 */
private static void CLEAR_HASH(deflate_state s) {
    s.head_array[s.head_index + s.hash_size-1] = NIL;
    long sz = (uint)(s.hash_size - 1);
    for (long i = 0; i < sz; ++i) {
        s.head_array[s.head_index + i] = 0;
    }
}

/* ===========================================================================
 * Slide the hash table when sliding the window down (could be avoided with 32
 * bit values at the expense of memory usage). We slide even when level == 0 to
 * keep the hash table consistent if we switch back to level > 0 later.
 */
private static void slide_hash(deflate_state s) {
    uint n, m;
    ushort[] p_a;
    long p_i;
    uint wsize = s.w_size;

    n = s.hash_size;
    p_a = s.head_array;
    p_i = s.head_index + n;
    do {
        m = p_a[--p_i];
        p_a[p_i] = (ushort)(m >= wsize ? m - wsize : NIL);
    } while (--n != 0);
    n = wsize;
//#ifndef FASTEST
    p_a = s.prev_array;
    p_i = s.prev_index + n;
    do {
        m = p_a[--p_i];
        p_a[p_i] = (ushort)(m >= wsize ? m - wsize : NIL);
        /* If n is not on any hash chain, prev[n] is garbage but
         * its value will never be used.
         */
    } while (--n != 0);
//#endif
}

/* ========================================================================= */
public static int deflateInit_(
    z_stream strm,
    int level,
    string version,
    int stream_size)
{
    return deflateInit2_(strm, level, zlib.Z_DEFLATED, zconf.MAX_WBITS, zutil.DEF_MEM_LEVEL,
                         zlib.Z_DEFAULT_STRATEGY, version, stream_size);
    /* To do: ignore strm->next_in if we use it as window */
}

/* ========================================================================= */
public static int deflateInit2_(
    z_stream strm,
    int  level,
    int  method,
    int  windowBits,
    int  memLevel,
    int  strategy,
    string version,
    int stream_size)
{
    deflate_state s;
    int wrap = 1;
//    string my_version = zlib.ZLIB_VERSION;

    byte[] overlay;
    /* We overlay pending_buf and d_buf+l_buf. This works since the average
     * output size for (length,distance) codes is <= 24 bits.
     */

    if (version == null || version.Length == 0 || version[0] != zlib.ZLIB_VERSION[0] ||
        stream_size != z_stream._sizeof) {
        return zlib.Z_VERSION_ERROR;
    }
    if (strm == null) return zlib.Z_STREAM_ERROR;

    strm.msg = null;
//#ifdef FASTEST
//    if (level != 0) level = 1;
//#else
    if (level == zlib.Z_DEFAULT_COMPRESSION) level = 6;
//#endif

    if (windowBits < 0) { /* suppress zlib wrapper */
        wrap = 0;
        windowBits = -windowBits;
    }
//#ifdef GZIP
    else if (windowBits > 15) {
        wrap = 2;       /* write gzip wrapper instead */
        windowBits -= 16;
    }
//#endif
    if (memLevel < 1 || memLevel > zconf.MAX_MEM_LEVEL || method != zlib.Z_DEFLATED ||
        windowBits < 8 || windowBits > 15 || level < 0 || level > 9 ||
        strategy < 0 || strategy > zlib.Z_FIXED || (windowBits == 8 && wrap != 1)) {
        return zlib.Z_STREAM_ERROR;
    }
    if (windowBits == 8) windowBits = 9;  /* until 256-byte window bug fixed */
    s = new deflate_state();
    if (s == null) return zlib.Z_MEM_ERROR;
    strm.dstate = s;
    s.strm = strm;
    s.status = INIT_STATE;     /* to pass state test in deflateReset() */

    s.wrap = wrap;
    s.gzhead = null;
    s.w_bits = (uint)windowBits;
    s.w_size = 1u << (int)s.w_bits;
    s.w_mask = s.w_size - 1;

    s.hash_bits = (uint)memLevel + 7;
    s.hash_size = 1u << (int)s.hash_bits;
    s.hash_mask = s.hash_size - 1;
    s.hash_shift = (int)((s.hash_bits+zutil.MIN_MATCH-1)/zutil.MIN_MATCH);

    s.window_array = new byte[s.w_size * 2];
    s.window_index = 0;
    s.prev_array   = new ushort[s.w_size];
    s.prev_index   = 0;
    s.head_array   = new ushort[s.hash_size];
    s.head_index   = 0;

    s.high_water = 0;      /* nothing written to s.window yet */

    s.lit_bufsize = 1u << (memLevel + 6); /* 16K elements by default */

    overlay = new byte[s.lit_bufsize * 4];
    s.pending_buf = overlay;
    s.pending_buf_size = (ulong)s.lit_bufsize * 4ul;

    if (s.window_array == null || s.prev_array == null || s.head_array == null ||
        s.pending_buf == null) {
        s.status = FINISH_STATE;
        strm.msg = zutil.ERR_MSG(zlib.Z_MEM_ERROR);
        deflateEnd (strm);
        return zlib.Z_MEM_ERROR;
    }
    s.d_buf = new ushort_array_from_byte_array(overlay, s.lit_bufsize);
    s.l_buf_array = s.pending_buf;
    s.l_buf_index = 3 * s.lit_bufsize;

    s.level = level;
    s.strategy = strategy;
    s.method = (byte)method;

    return deflateReset(strm);
}

/* =========================================================================
 * Check for a valid deflate stream state. Return 0 if ok, 1 if not.
 */
private static int deflateStateCheck(z_stream strm) {
    deflate_state s;
    if (strm == null)
        return 1;
    s = strm.dstate;
    if (s == null || s.strm != strm || (s.status != INIT_STATE &&
//#ifdef GZIP
                                           s.status != GZIP_STATE &&
//#endif
                                           s.status != EXTRA_STATE &&
                                           s.status != NAME_STATE &&
                                           s.status != COMMENT_STATE &&
                                           s.status != HCRC_STATE &&
                                           s.status != BUSY_STATE &&
                                           s.status != FINISH_STATE))
        return 1;
    return 0;
}

/* ========================================================================= */
//int ZEXPORT deflateSetDictionary (strm, dictionary, dictLength)
//    z_streamp strm;
//    const Bytef *dictionary;
//    uint  dictLength;
//{
//    deflate_state *s;
//    uint str, n;
//    int wrap;
//    uint avail;
//    z_const uint char *next;
//
//    if (deflateStateCheck(strm) || dictionary == Z_NULL)
//        return Z_STREAM_ERROR;
//    s = strm->state;
//    wrap = s->wrap;
//    if (wrap == 2 || (wrap == 1 && s->status != INIT_STATE) || s->lookahead)
//        return Z_STREAM_ERROR;
//
//    /* when using zlib wrappers, compute Adler-32 for provided dictionary */
//    if (wrap == 1)
//        strm->adler = adler32(strm->adler, dictionary, dictLength);
//    s->wrap = 0;                    /* avoid computing Adler-32 in read_buf */
//
//    /* if dictionary would fill window, just replace the history */
//    if (dictLength >= s->w_size) {
//        if (wrap == 0) {            /* already empty otherwise */
//            CLEAR_HASH(s);
//            s->strstart = 0;
//            s->block_start = 0L;
//            s->insert = 0;
//        }
//        dictionary += dictLength - s->w_size;  /* use the tail */
//        dictLength = s->w_size;
//    }
//
//    /* insert dictionary into window and hash */
//    avail = strm->avail_in;
//    next = strm->next_in;
//    strm->avail_in = dictLength;
//    strm->next_in = (z_const Bytef *)dictionary;
//    fill_window(s);
//    while (s->lookahead >= MIN_MATCH) {
//        str = s->strstart;
//        n = s->lookahead - (MIN_MATCH-1);
//        do {
//            UPDATE_HASH(s, s->ins_h, s->window[str + MIN_MATCH-1]);
//#ifndef FASTEST
//            s->prev[str & s->w_mask] = s->head[s->ins_h];
//#endif
//            s->head[s->ins_h] = (Pos)str;
//            str++;
//        } while (--n);
//        s->strstart = str;
//        s->lookahead = MIN_MATCH-1;
//        fill_window(s);
//    }
//    s->strstart += s->lookahead;
//    s->block_start = (long)s->strstart;
//    s->insert = s->lookahead;
//    s->lookahead = 0;
//    s->match_length = s->prev_length = MIN_MATCH-1;
//    s->match_available = 0;
//    strm->next_in = next;
//    strm->avail_in = avail;
//    s->wrap = wrap;
//    return Z_OK;
//}
//
///* ========================================================================= */
//int ZEXPORT deflateGetDictionary (strm, dictionary, dictLength)
//    z_streamp strm;
//    Bytef *dictionary;
//    uint  *dictLength;
//{
//    deflate_state *s;
//    uint len;
//
//    if (deflateStateCheck(strm))
//        return Z_STREAM_ERROR;
//    s = strm->state;
//    len = s->strstart + s->lookahead;
//    if (len > s->w_size)
//        len = s->w_size;
//    if (dictionary != Z_NULL && len)
//        zmemcpy(dictionary, s->window + s->strstart + s->lookahead - len, len);
//    if (dictLength != Z_NULL)
//        *dictLength = len;
//    return Z_OK;
//}

/* ========================================================================= */
public static int deflateResetKeep(z_stream strm) {
    deflate_state s;

    if (deflateStateCheck(strm) != 0) {
        return zlib.Z_STREAM_ERROR;
    }

    strm.total_in = strm.total_out = 0;
    strm.msg = null; /* use zfree if we ever allocate msg dynamically */
    strm.data_type = zlib.Z_UNKNOWN;

    s = strm.dstate;
    s.pending = 0;
    s.pending_out_array = s.pending_buf;
    s.pending_out_index = 0;

    if (s.wrap < 0) {
        s.wrap = -s.wrap; /* was made negative by deflate(..., Z_FINISH); */
    }
    s.status =
//#ifdef GZIP
        s.wrap == 2 ? GZIP_STATE :
//#endif
        s.wrap != 0 ? INIT_STATE : BUSY_STATE;
    strm.adler =
//#ifdef GZIP
        s.wrap == 2 ? crc32.crc32_(0L, null, 0, 0) :
//#endif
        adler32.adler32_(0L, null, 0, 0);
    s.last_flush = zlib.Z_NO_FLUSH;

    trees._tr_init(s);

    return zlib.Z_OK;
}

/* ========================================================================= */
public static int deflateReset(z_stream strm) {
    int ret;

    ret = deflateResetKeep(strm);
    if (ret == zlib.Z_OK)
        lm_init(strm.dstate);
    return ret;
}

/* ========================================================================= */
//int ZEXPORT deflateSetHeader (strm, head)
//    z_streamp strm;
//    gz_headerp head;
//{
//    if (deflateStateCheck(strm) || strm->state->wrap != 2)
//        return Z_STREAM_ERROR;
//    strm->state->gzhead = head;
//    return Z_OK;
//}
//
///* ========================================================================= */
//int ZEXPORT deflatePending (strm, pending, bits)
//    uint *pending;
//    int *bits;
//    z_streamp strm;
//{
//    if (deflateStateCheck(strm)) return Z_STREAM_ERROR;
//    if (pending != Z_NULL)
//        *pending = strm->state->pending;
//    if (bits != Z_NULL)
//        *bits = strm->state->bi_valid;
//    return Z_OK;
//}
//
///* ========================================================================= */
//int ZEXPORT deflatePrime (strm, bits, value)
//    z_streamp strm;
//    int bits;
//    int value;
//{
//    deflate_state *s;
//    int put;
//
//    if (deflateStateCheck(strm)) return Z_STREAM_ERROR;
//    s = strm->state;
//    if ((Bytef *)(s->d_buf) < s->pending_out + ((Buf_size + 7) >> 3))
//        return Z_BUF_ERROR;
//    do {
//        put = Buf_size - s->bi_valid;
//        if (put > bits)
//            put = bits;
//        s->bi_buf |= (ush)((value & ((1 << put) - 1)) << s->bi_valid);
//        s->bi_valid += put;
//        _tr_flush_bits(s);
//        value >>= put;
//        bits -= put;
//    } while (bits);
//    return Z_OK;
//}
//
///* ========================================================================= */
//int ZEXPORT deflateParams(strm, level, strategy)
//    z_streamp strm;
//    int level;
//    int strategy;
//{
//    deflate_state *s;
//    compress_func func;
//
//    if (deflateStateCheck(strm)) return Z_STREAM_ERROR;
//    s = strm->state;
//
//#ifdef FASTEST
//    if (level != 0) level = 1;
//#else
//    if (level == Z_DEFAULT_COMPRESSION) level = 6;
//#endif
//    if (level < 0 || level > 9 || strategy < 0 || strategy > Z_FIXED) {
//        return Z_STREAM_ERROR;
//    }
//    func = configuration_table[s->level].func;
//
//    if ((strategy != s->strategy || func != configuration_table[level].func) &&
//        s->high_water) {
//        /* Flush the last buffer: */
//        int err = deflate(strm, Z_BLOCK);
//        if (err == Z_STREAM_ERROR)
//            return err;
//        if (strm->avail_out == 0)
//            return Z_BUF_ERROR;
//    }
//    if (s->level != level) {
//        if (s->level == 0 && s->matches != 0) {
//            if (s->matches == 1)
//                slide_hash(s);
//            else
//                CLEAR_HASH(s);
//            s->matches = 0;
//        }
//        s->level = level;
//        s->max_lazy_match   = configuration_table[level].max_lazy;
//        s->good_match       = configuration_table[level].good_length;
//        s->nice_match       = configuration_table[level].nice_length;
//        s->max_chain_length = configuration_table[level].max_chain;
//    }
//    s->strategy = strategy;
//    return Z_OK;
//}
//
///* ========================================================================= */
//int ZEXPORT deflateTune(strm, good_length, max_lazy, nice_length, max_chain)
//    z_streamp strm;
//    int good_length;
//    int max_lazy;
//    int nice_length;
//    int max_chain;
//{
//    deflate_state *s;
//
//    if (deflateStateCheck(strm)) return Z_STREAM_ERROR;
//    s = strm->state;
//    s->good_match = (uint)good_length;
//    s->max_lazy_match = (uint)max_lazy;
//    s->nice_match = nice_length;
//    s->max_chain_length = (uint)max_chain;
//    return Z_OK;
//}

/* =========================================================================
 * For the default windowBits of 15 and memLevel of 8, this function returns
 * a close to exact, as well as small, upper bound on the compressed size.
 * They are coded as constants here for a reason--if the #define's are
 * changed, then this function needs to be changed as well.  The return
 * value for 15 and 8 only works for those exact settings.
 *
 * For any setting other than those defaults for windowBits and memLevel,
 * the value returned is a conservative worst case for the maximum expansion
 * resulting from using fixed blocks instead of stored blocks, which deflate
 * can emit on compressed data for some combinations of the parameters.
 *
 * This function could be more sophisticated to provide closer upper bounds for
 * every combination of windowBits and memLevel.  But even the conservative
 * upper bound of about 14% expansion does not seem onerous for output buffer
 * allocation.
 */
public static ulong deflateBound(
    z_stream strm,
    ulong sourceLen)
{
    deflate_state s;
    ulong complen, wraplen;

    /* conservative upper bound for compressed data */
    complen = sourceLen +
              ((sourceLen + 7) >> 3) + ((sourceLen + 63) >> 6) + 5;

    /* if can't get parameters, return conservative bound plus zlib wrapper */
    if (deflateStateCheck(strm) != 0)
        return complen + 6;

    /* compute wrapper length */
    s = strm.dstate;
    switch (s.wrap) {
    case 0:                                 /* raw deflate */
        wraplen = 0;
        break;
    case 1:                                 /* zlib wrapper */
        wraplen = 6u + (s.strstart != 0 ? 4u : 0u);
        break;
//#ifdef GZIP
    case 2:                                 /* gzip wrapper */
        wraplen = 18;
        if (s.gzhead != null) {          /* user-supplied gzip header */
            byte[] str_array;
            long str_index;
            if (s.gzhead.extra != null)
                wraplen += 2 + s.gzhead.extra_len;
            str_array = s.gzhead.name;
            str_index = 0;
            if (str_array != null)
                do {
                    wraplen++;
                } while (str_array[str_index++] != 0);
            str_array = s.gzhead.comment;
            str_index = 0;
            if (str_array != null)
                do {
                    wraplen++;
                } while (str_array[str_index++] != 0);
            if (s.gzhead.hcrc != 0)
                wraplen += 2;
        }
        break;
//#endif
    default:                                /* for compiler happiness */
        wraplen = 6;
        break;
    }

    /* if not default parameters, return conservative bound */
    if (s.w_bits != 15 || s.hash_bits != 8 + 7)
        return complen + wraplen;

    /* default settings: return tight bound for that case */
    return sourceLen + (sourceLen >> 12) + (sourceLen >> 14) +
           (sourceLen >> 25) + 13 - 6 + wraplen;
}

/* =========================================================================
 * Put a short in the pending buffer. The 16-bit value is put in MSB order.
 * IN assertion: the stream state is correct and there is enough room in
 * pending_buf.
 */
private static void putShortMSB(deflate_state s, uint b) {
    put_byte(s, (byte)((b >> 8) & 0xff));
    put_byte(s, (byte)(b & 0xff));
}

/* =========================================================================
 * Flush as much pending output as possible. All deflate() output, except for
 * some deflate_stored() output, goes through this function so some
 * applications may wish to modify it to avoid allocating a large
 * strm->next_out buffer and copying into it. (See also read_buf()).
 */
private static void flush_pending(z_stream strm) {
    uint len;
    deflate_state s = strm.dstate;

    trees._tr_flush_bits(s);
    len = s.pending > (ulong)strm.avail_out ? strm.avail_out : (uint)s.pending;
//    if (len > strm.avail_out) len = strm.avail_out;
    if (len == 0) return;

    zutil.zmemcpy(strm.output_buffer, strm.next_out, s.pending_out_array, s.pending_out_index, len);
    strm.next_out  += len;
    s.pending_out_index  += len;
    strm.total_out += len;
    strm.avail_out -= len;
    s.pending      -= len;
    if (s.pending == 0) {
        s.pending_out_array = s.pending_buf;
        s.pending_out_index = 0;
    }
}

/* ===========================================================================
 * Update the header CRC with the bytes s->pending_buf[beg..s->pending - 1].
 */
private static void HCRC_UPDATE(z_stream strm, deflate_state s, ulong beg) {
    do {
        if (s.gzhead.hcrc != 0 && s.pending > (beg))
            strm.adler = crc32.crc32_(strm.adler, s.pending_buf, ((long)beg),
                                (uint)(s.pending - (beg)));
    } while (false);
}

/* ========================================================================= */
public static int deflate_(z_stream strm, int flush) {
    int old_flush; /* value of flush param for previous deflate call */
    deflate_state s;

    if (deflateStateCheck(strm) != 0 || flush > zlib.Z_BLOCK || flush < 0) {
        return zlib.Z_STREAM_ERROR;
    }
    s = strm.dstate;

    if (strm.output_buffer == null ||
        (strm.avail_in != 0 && strm.input_buffer == null) ||
        (s.status == FINISH_STATE && flush != zlib.Z_FINISH)) {
        strm.msg = zutil.ERR_MSG(zlib.Z_STREAM_ERROR);
        return zlib.Z_STREAM_ERROR;
    }
    if (strm.avail_out == 0) {
        strm.msg = zutil.ERR_MSG(zlib.Z_BUF_ERROR);
        return zlib.Z_BUF_ERROR;
    }

    old_flush = s.last_flush;
    s.last_flush = flush;

    /* Flush as much pending output as possible */
    if (s.pending != 0) {
        flush_pending(strm);
        if (strm.avail_out == 0) {
            /* Since avail_out is 0, deflate will be called again with
             * more output space, but possibly with both pending and
             * avail_in equal to zero. There won't be anything to do,
             * but this is not an error situation so make sure we
             * return OK instead of BUF_ERROR at next call of deflate:
             */
            s.last_flush = -1;
            return zlib.Z_OK;
        }

    /* Make sure there is something to do and avoid duplicate consecutive
     * flushes. For repeated and useless calls with Z_FINISH, we keep
     * returning Z_STREAM_END instead of Z_BUF_ERROR.
     */
    } else if (strm.avail_in == 0 && RANK(flush) <= RANK(old_flush) &&
               flush != zlib.Z_FINISH) {
        strm.msg = zutil.ERR_MSG(zlib.Z_BUF_ERROR);
        return zlib.Z_BUF_ERROR;
    }

    /* User must not provide more input after the first FINISH: */
    if (s.status == FINISH_STATE && strm.avail_in != 0) {
        strm.msg = zutil.ERR_MSG(zlib.Z_BUF_ERROR);
        return zlib.Z_BUF_ERROR;
    }

    /* Write the header */
    if (s.status == INIT_STATE) {
        /* zlib header */
        uint header = (zlib.Z_DEFLATED + ((s.w_bits-8)<<4)) << 8;
        uint level_flags;

        if (s.strategy >= zlib.Z_HUFFMAN_ONLY || s.level < 2)
            level_flags = 0;
        else if (s.level < 6)
            level_flags = 1;
        else if (s.level == 6)
            level_flags = 2;
        else
            level_flags = 3;
        header |= (level_flags << 6);
        if (s.strstart != 0) header |= zutil.PRESET_DICT;
        header += 31 - (header % 31);

        putShortMSB(s, header);

        /* Save the adler32 of the preset dictionary: */
        if (s.strstart != 0) {
            putShortMSB(s, (uint)(strm.adler >> 16));
            putShortMSB(s, (uint)(strm.adler & 0xffff));
        }
        strm.adler = adler32.adler32_(0L, null, 0, 0);
        s.status = BUSY_STATE;

        /* Compression must start with an empty pending buffer */
        flush_pending(strm);
        if (s.pending != 0) {
            s.last_flush = -1;
            return zlib.Z_OK;
        }
    }
//#ifdef GZIP
    if (s.status == GZIP_STATE) {
        /* gzip header */
        strm.adler = crc32.crc32_(0L, null, 0, 0);
        put_byte(s, 31);
        put_byte(s, 139);
        put_byte(s, 8);
        if (s.gzhead == null) {
            put_byte(s, 0);
            put_byte(s, 0);
            put_byte(s, 0);
            put_byte(s, 0);
            put_byte(s, 0);
            put_byte(s, s.level == 9 ? (byte)2 :
                     (s.strategy >= zlib.Z_HUFFMAN_ONLY || s.level < 2 ?
                      (byte)4 : (byte)0));
            put_byte(s, zutil.OS_CODE);
            s.status = BUSY_STATE;

            /* Compression must start with an empty pending buffer */
            flush_pending(strm);
            if (s.pending != 0) {
                s.last_flush = -1;
                return zlib.Z_OK;
            }
        }
        else {
            put_byte(s, (byte)((s.gzhead.text != 0 ? 1 : 0) +
                     (s.gzhead.hcrc != 0 ? 2 : 0) +
                     (s.gzhead.extra == null ? 0 : 4) +
                     (s.gzhead.name == null ? 0 : 8) +
                     (s.gzhead.comment == null ? 0 : 16))
                     );
            put_byte(s, (byte)(s.gzhead.time & 0xff));
            put_byte(s, (byte)((s.gzhead.time >> 8) & 0xff));
            put_byte(s, (byte)((s.gzhead.time >> 16) & 0xff));
            put_byte(s, (byte)((s.gzhead.time >> 24) & 0xff));
            put_byte(s, s.level == 9 ? (byte)2 :
                     (s.strategy >= zlib.Z_HUFFMAN_ONLY || s.level < 2 ?
                      (byte)4 : (byte)0));
            put_byte(s, (byte)(s.gzhead.os & 0xff));
            if (s.gzhead.extra != null) {
                put_byte(s, (byte)(s.gzhead.extra_len & 0xff));
                put_byte(s, (byte)((s.gzhead.extra_len >> 8) & 0xff));
            }
            if (s.gzhead.hcrc != 0)
                strm.adler = crc32.crc32_(strm.adler, s.pending_buf, 0, (uint)s.pending);
            s.gzindex = 0;
            s.status = EXTRA_STATE;
        }
    }
    if (s.status == EXTRA_STATE) {
        if (s.gzhead.extra != null) {
            ulong beg = s.pending;   /* start of bytes to update crc */
            uint left = (uint)((s.gzhead.extra_len & 0xffff) - s.gzindex);
            while (s.pending + left > s.pending_buf_size) {
                uint copy = (uint)(s.pending_buf_size - s.pending);
                zutil.zmemcpy(s.pending_buf, (long)s.pending, s.gzhead.extra, (long)s.gzindex, copy);
                s.pending = s.pending_buf_size;
                HCRC_UPDATE(strm, s, beg);
                s.gzindex += copy;
                flush_pending(strm);
                if (s.pending != 0) {
                    s.last_flush = -1;
                    return zlib.Z_OK;
                }
                beg = 0;
                left -= copy;
            }
            zutil.zmemcpy(s.pending_buf, (long)s.pending, s.gzhead.extra, (long)s.gzindex, left);
            s.pending += left;
            HCRC_UPDATE(strm, s, beg);
            s.gzindex = 0;
        }
        s.status = NAME_STATE;
    }
    if (s.status == NAME_STATE) {
        if (s.gzhead.name != null) {
            ulong beg = s.pending;   /* start of bytes to update crc */
            byte val;
            do {
                if (s.pending == s.pending_buf_size) {
                    HCRC_UPDATE(strm, s, beg);
                    flush_pending(strm);
                    if (s.pending != 0) {
                        s.last_flush = -1;
                        return zlib.Z_OK;
                    }
                    beg = 0;
                }
                val = s.gzhead.name[s.gzindex++];
                put_byte(s, val);
            } while (val != 0);
            HCRC_UPDATE(strm, s, beg);
            s.gzindex = 0;
        }
        s.status = COMMENT_STATE;
    }
    if (s.status == COMMENT_STATE) {
        if (s.gzhead.comment != null) {
            ulong beg = s.pending;   /* start of bytes to update crc */
            byte val;
            do {
                if (s.pending == s.pending_buf_size) {
                    HCRC_UPDATE(strm, s, beg);
                    flush_pending(strm);
                    if (s.pending != 0) {
                        s.last_flush = -1;
                        return zlib.Z_OK;
                    }
                    beg = 0;
                }
                val = s.gzhead.comment[s.gzindex++];
                put_byte(s, val);
            } while (val != 0);
            HCRC_UPDATE(strm, s, beg);
        }
        s.status = HCRC_STATE;
    }
    if (s.status == HCRC_STATE) {
        if (s.gzhead.hcrc != 0) {
            if (s.pending + 2 > s.pending_buf_size) {
                flush_pending(strm);
                if (s.pending != 0) {
                    s.last_flush = -1;
                    return zlib.Z_OK;
                }
            }
            put_byte(s, (byte)(strm.adler & 0xff));
            put_byte(s, (byte)((strm.adler >> 8) & 0xff));
            strm.adler = crc32.crc32_(0L, null, 0, 0);
        }
        s.status = BUSY_STATE;

        /* Compression must start with an empty pending buffer */
        flush_pending(strm);
        if (s.pending != 0) {
            s.last_flush = -1;
            return zlib.Z_OK;
        }
    }
//#endif

    /* Start a new block or continue the current one.
     */
    if (strm.avail_in != 0 || s.lookahead != 0 ||
        (flush != zlib.Z_NO_FLUSH && s.status != FINISH_STATE)) {
        block_state bstate;

        bstate = s.level == 0 ? deflate_stored(s, flush) :
                 s.strategy == zlib.Z_HUFFMAN_ONLY ? deflate_huff(s, flush) :
                 s.strategy == zlib.Z_RLE ? deflate_rle(s, flush) :
                 configuration_table[s.level].func(s, flush);

        if (bstate == block_state.finish_started || bstate == block_state.finish_done) {
            s.status = FINISH_STATE;
        }
        if (bstate == block_state.need_more || bstate == block_state.finish_started) {
            if (strm.avail_out == 0) {
                s.last_flush = -1; /* avoid BUF_ERROR next call, see above */
            }
            return zlib.Z_OK;
            /* If flush != Z_NO_FLUSH && avail_out == 0, the next call
             * of deflate should use the same flush parameter to make sure
             * that the flush is complete. So we don't have to output an
             * empty block here, this will be done at next call. This also
             * ensures that for a very small output buffer, we emit at most
             * one empty block.
             */
        }
        if (bstate == block_state.block_done) {
            if (flush == zlib.Z_PARTIAL_FLUSH) {
                trees._tr_align(s);
            } else if (flush != zlib.Z_BLOCK) { /* FULL_FLUSH or SYNC_FLUSH */
                trees._tr_stored_block(s, null, 0, 0, 0);
                /* For a full flush, this empty block will be recognized
                 * as a special marker by inflate_sync().
                 */
                if (flush == zlib.Z_FULL_FLUSH) {
                    CLEAR_HASH(s);             /* forget history */
                    if (s.lookahead == 0) {
                        s.strstart = 0;
                        s.block_start = 0L;
                        s.insert = 0;
                    }
                }
            }
            flush_pending(strm);
            if (strm.avail_out == 0) {
              s.last_flush = -1; /* avoid BUF_ERROR at next call, see above */
              return zlib.Z_OK;
            }
        }
    }

    if (flush != zlib.Z_FINISH) return zlib.Z_OK;
    if (s.wrap <= 0) return zlib.Z_STREAM_END;

    /* Write the trailer */
//#ifdef GZIP
    if (s.wrap == 2) {
        put_byte(s, (byte)(strm.adler & 0xff));
        put_byte(s, (byte)((strm.adler >> 8) & 0xff));
        put_byte(s, (byte)((strm.adler >> 16) & 0xff));
        put_byte(s, (byte)((strm.adler >> 24) & 0xff));
        put_byte(s, (byte)(strm.total_in & 0xff));
        put_byte(s, (byte)((strm.total_in >> 8) & 0xff));
        put_byte(s, (byte)((strm.total_in >> 16) & 0xff));
        put_byte(s, (byte)((strm.total_in >> 24) & 0xff));
    }
    else
//#endif
    {
        putShortMSB(s, (uint)(strm.adler >> 16));
        putShortMSB(s, (uint)(strm.adler & 0xffff));
    }
    flush_pending(strm);
    /* If avail_out is zero, the application will call deflate again
     * to flush the rest.
     */
    if (s.wrap > 0) s.wrap = -s.wrap; /* write the trailer only once! */
    return s.pending != 0 ? zlib.Z_OK : zlib.Z_STREAM_END;
}

/* ========================================================================= */
public static int deflateEnd (
    z_stream strm)
{
    int status;

    if (deflateStateCheck(strm) != 0) return zlib.Z_STREAM_ERROR;

    status = strm.dstate.status;

    /* Deallocate in reverse order of allocations: */
    strm.dstate.pending_buf = null;
    strm.dstate.head_array = null;
    strm.dstate.prev_array = null;
    strm.dstate.window_array = null;

    strm.dstate = null;

    return status == BUSY_STATE ? zlib.Z_DATA_ERROR : zlib.Z_OK;
}

///* =========================================================================
// * Copy the source state to the destination state.
// * To simplify the source, this is not supported for 16-bit MSDOS (which
// * doesn't have enough memory anyway to duplicate compression states).
// */
//int ZEXPORT deflateCopy (dest, source)
//    z_streamp dest;
//    z_streamp source;
//{
//#ifdef MAXSEG_64K
//    return Z_STREAM_ERROR;
//#else
//    deflate_state *ds;
//    deflate_state *ss;
//    ushf *overlay;
//
//
//    if (deflateStateCheck(source) || dest == Z_NULL) {
//        return Z_STREAM_ERROR;
//    }
//
//    ss = source->state;
//
//    zmemcpy((voidpf)dest, (voidpf)source, sizeof(z_stream));
//
//    ds = (deflate_state *) ZALLOC(dest, 1, sizeof(deflate_state));
//    if (ds == Z_NULL) return Z_MEM_ERROR;
//    dest->state = (struct internal_state FAR *) ds;
//    zmemcpy((voidpf)ds, (voidpf)ss, sizeof(deflate_state));
//    ds->strm = dest;
//
//    ds->window = (Bytef *) ZALLOC(dest, ds->w_size, 2*sizeof(byte));
//    ds->prev   = (Posf *)  ZALLOC(dest, ds->w_size, sizeof(Pos));
//    ds->head   = (Posf *)  ZALLOC(dest, ds->hash_size, sizeof(Pos));
//    overlay = (ushf *) ZALLOC(dest, ds->lit_bufsize, sizeof(ush)+2);
//    ds->pending_buf = (uchf *) overlay;
//
//    if (ds->window == Z_NULL || ds->prev == Z_NULL || ds->head == Z_NULL ||
//        ds->pending_buf == Z_NULL) {
//        deflateEnd (dest);
//        return Z_MEM_ERROR;
//    }
//    /* following zmemcpy do not work for 16-bit MSDOS */
//    zmemcpy(ds->window, ss->window, ds->w_size * 2 * sizeof(byte));
//    zmemcpy((voidpf)ds->prev, (voidpf)ss->prev, ds->w_size * sizeof(Pos));
//    zmemcpy((voidpf)ds->head, (voidpf)ss->head, ds->hash_size * sizeof(Pos));
//    zmemcpy(ds->pending_buf, ss->pending_buf, (uint)ds->pending_buf_size);
//
//    ds->pending_out = ds->pending_buf + (ss->pending_out - ss->pending_buf);
//    ds->d_buf = overlay + ds->lit_bufsize/sizeof(ush);
//    ds->l_buf = ds->pending_buf + (1+sizeof(ush))*ds->lit_bufsize;
//
//    ds->l_desc.dyn_tree = ds->dyn_ltree;
//    ds->d_desc.dyn_tree = ds->dyn_dtree;
//    ds->bl_desc.dyn_tree = ds->bl_tree;
//
//    return Z_OK;
//#endif /* MAXSEG_64K */
//}

/* ===========================================================================
 * Read a new buffer from the current input stream, update the adler32
 * and total number of bytes read.  All deflate() input goes through
 * this function so some applications may wish to modify it to avoid
 * allocating a large strm->next_in buffer and copying from it.
 * (See also flush_pending()).
 */
private static uint read_buf(
    z_stream strm,
    byte[] buf_array,
    long buf_index,
    uint size)
{
    uint len = strm.avail_in;

    if (len > size) len = size;
    if (len == 0) return 0;

    strm.avail_in  -= len;

    zutil.zmemcpy(buf_array, buf_index, strm.input_buffer, strm.next_in, len);
    if (strm.dstate.wrap == 1) {
        strm.adler = adler32.adler32_(strm.adler, buf_array, buf_index, len);
    }
//#ifdef GZIP
    else if (strm.dstate.wrap == 2) {
        strm.adler = crc32.crc32_(strm.adler, buf_array, buf_index, len);
    }
//#endif
    strm.next_in  += len;
    strm.total_in += len;

    return len;
}

///* ===========================================================================
// * Initialize the "longest match" routines for a new zlib stream
// */
private static void lm_init(deflate_state s) {
    s.window_size = (ulong)2L*s.w_size;

    CLEAR_HASH(s);

    /* Set the default configuration parameters:
     */
    s.max_lazy_match   = configuration_table[s.level].max_lazy;
    s.good_match       = configuration_table[s.level].good_length;
    s.nice_match       = configuration_table[s.level].nice_length;
    s.max_chain_length = configuration_table[s.level].max_chain;

    s.strstart = 0;
    s.block_start = 0L;
    s.lookahead = 0;
    s.insert = 0;
    s.match_length = s.prev_length = zutil.MIN_MATCH-1;
    s.match_available = 0;
    s.ins_h = 0;
//#ifndef FASTEST
//#ifdef ASMV
//    match_init(); /* initialize the asm code */
//#endif
//#endif
}

//#ifndef FASTEST
/* ===========================================================================
 * Set match_start to the longest match starting at the given string and
 * return its length. Matches shorter or equal to prev_length are discarded,
 * in which case the result is equal to prev_length and match_start is
 * garbage.
 * IN assertions: cur_match is the head of the hash chain for the current
 *   string (strstart) and its distance is <= MAX_DIST, and prev_length >= 1
 * OUT assertion: the match length is not greater than s->lookahead.
 */
//#ifndef ASMV
/* For 80x86 and 680x0, an optimized version will be provided in match.asm or
 * match.S. The code will be functionally equivalent.
 */
private static uint longest_match(
    deflate_state s,
    uint cur_match)                             /* current match */
{
    uint chain_length = s.max_chain_length;/* max hash chain length */
    byte[] scan_array = s.window_array; /* current string */
    long scan_index = s.window_index + s.strstart;
    byte[] match_array;                      /* matched string */
    long match_index;
    int len;                           /* length of current match */
    int best_len = (int)s.prev_length;         /* best match length so far */
    int nice_match = s.nice_match;             /* stop if match long enough */
    uint limit = s.strstart > (uint)MAX_DIST(s) ?
        s.strstart - (uint)MAX_DIST(s) : NIL;
    /* Stop when cur_match becomes <= limit. To simplify the code,
     * we prevent matches with the string of window index 0.
     */
    ushort[] prev_array = s.prev_array;
    long prev_index = s.prev_index;
    uint wmask = s.w_mask;

//#ifdef UNALIGNED_OK
//    /* Compare two bytes at a time. Note: this is not always beneficial.
//     * Try with and without -DUNALIGNED_OK to check.
//     */
//    register Bytef *strend = s->window + s->strstart + MAX_MATCH - 1;
//    register ush scan_start = *(ushf*)scan;
//    register ush scan_end   = *(ushf*)(scan+best_len-1);
//#else
    byte[] strend_array = s.window_array;
    long strend_index = s.window_index + s.strstart + zutil.MAX_MATCH;
    byte scan_end1  = scan_array[scan_index + best_len-1];
    byte scan_end   = scan_array[scan_index + best_len];
//#endif

    /* The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple of 16.
     * It is easy to get rid of this optimization if necessary.
     */
    //Assert(s->hash_bits >= 8 && MAX_MATCH == 258, "Code too clever");

    /* Do not waste too much time if we already have a good match: */
    if (s.prev_length >= s.good_match) {
        chain_length >>= 2;
    }
    /* Do not look for matches beyond the end of the input. This is necessary
     * to make deflate deterministic.
     */
    if ((uint)nice_match > s.lookahead) nice_match = (int)s.lookahead;

    //Assert((ulong)s->strstart <= s->window_size-MIN_LOOKAHEAD, "need lookahead");

    do {
        //Assert(cur_match < s->strstart, "no future");
        match_array = s.window_array;
        match_index = s.window_index + cur_match;

        /* Skip to next match if the match length cannot increase
         * or if the match length is less than 2.  Note that the checks below
         * for insufficient lookahead only occur occasionally for performance
         * reasons.  Therefore uninitialized memory will be accessed, and
         * conditional jumps will be made that depend on those values.
         * However the length of the match is limited to the lookahead, so
         * the output of deflate is not affected by the uninitialized values.
         */
//#if (defined(UNALIGNED_OK) && MAX_MATCH == 258)
//        /* This code assumes sizeof(unsigned short) == 2. Do not use
//         * UNALIGNED_OK if your compiler uses a different size.
//         */
//        if (*(ushf*)(match+best_len-1) != scan_end ||
//            *(ushf*)match != scan_start) continue;
//
//        /* It is not necessary to compare scan[2] and match[2] since they are
//         * always equal when the other bytes match, given that the hash keys
//         * are equal and that HASH_BITS >= 8. Compare 2 bytes at a time at
//         * strstart+3, +5, ... up to strstart+257. We check for insufficient
//         * lookahead only every 4th comparison; the 128th check will be made
//         * at strstart+257. If MAX_MATCH-2 is not a multiple of 8, it is
//         * necessary to put more guard bytes at the end of the window, or
//         * to check more often for insufficient lookahead.
//         */
//        Assert(scan[2] == match[2], "scan[2]?");
//        scan++, match++;
//        do {
//        } while (*(ushf*)(scan+=2) == *(ushf*)(match+=2) &&
//                 *(ushf*)(scan+=2) == *(ushf*)(match+=2) &&
//                 *(ushf*)(scan+=2) == *(ushf*)(match+=2) &&
//                 *(ushf*)(scan+=2) == *(ushf*)(match+=2) &&
//                 scan < strend);
//        /* The funny "do {}" generates better code on most compilers */
//
//        /* Here, scan <= window+strstart+257 */
//        Assert(scan <= s->window+(unsigned)(s->window_size-1), "wild scan");
//        if (*scan == *match) scan++;
//
//        len = (MAX_MATCH - 1) - (int)(strend-scan);
//        scan = strend - (MAX_MATCH-1);
//
//#else /* UNALIGNED_OK */

        if (match_array[match_index + best_len]   != scan_end  ||
            match_array[match_index + best_len-1] != scan_end1 ||
            match_array[match_index]              != scan_array[scan_index] ||
            match_array[++match_index]            != scan_array[scan_index + 1])      continue;

        /* The check at best_len-1 can be removed because it will be made
         * again later. (This heuristic is not always a win.)
         * It is not necessary to compare scan[2] and match[2] since they
         * are always equal when the other bytes match, given that
         * the hash keys are equal and that HASH_BITS >= 8.
         */
        scan_index += 2; match_index++;
        //Assert(*scan == *match, "match[2]?");

        /* We check for insufficient lookahead only every 8th comparison;
         * the 256th check will be made at strstart+258.
         */
        do {
        } while (scan_array[++scan_index] == match_array[++match_index] && scan_array[++scan_index] == match_array[++match_index] &&
                 scan_array[++scan_index] == match_array[++match_index] && scan_array[++scan_index] == match_array[++match_index] &&
                 scan_array[++scan_index] == match_array[++match_index] && scan_array[++scan_index] == match_array[++match_index] &&
                 scan_array[++scan_index] == match_array[++match_index] && scan_array[++scan_index] == match_array[++match_index] &&
                 scan_index < strend_index);

        //Assert(scan <= s->window+(unsigned)(s->window_size-1), "wild scan");

        len = zutil.MAX_MATCH - (int)(strend_index - scan_index);
        scan_array = strend_array;
        scan_index = strend_index - zutil.MAX_MATCH;

//#endif /* UNALIGNED_OK */

        if (len > best_len) {
            s.match_start = cur_match;
            best_len = len;
            if (len >= nice_match) break;
//#ifdef UNALIGNED_OK
//            scan_end = *(ushf*)(scan+best_len-1);
//#else
            scan_end1  = scan_array[scan_index + best_len-1];
            scan_end   = scan_array[scan_index + best_len];
//#endif
        }
    } while ((cur_match = prev_array[prev_index + (cur_match & wmask)]) > limit
             && --chain_length != 0);

    if ((uint)best_len <= s.lookahead) return (uint)best_len;
    return s.lookahead;
}
//#endif /* ASMV */
//
//#else /* FASTEST */
//
///* ---------------------------------------------------------------------------
// * Optimized version for FASTEST only
// */
//local uint longest_match(s, cur_match)
//    deflate_state *s;
//    IPos cur_match;                             /* current match */
//{
//    register Bytef *scan = s->window + s->strstart; /* current string */
//    register Bytef *match;                       /* matched string */
//    register int len;                           /* length of current match */
//    register Bytef *strend = s->window + s->strstart + MAX_MATCH;
//
//    /* The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple of 16.
//     * It is easy to get rid of this optimization if necessary.
//     */
//    Assert(s->hash_bits >= 8 && MAX_MATCH == 258, "Code too clever");
//
//    Assert((ulong)s->strstart <= s->window_size-MIN_LOOKAHEAD, "need lookahead");
//
//    Assert(cur_match < s->strstart, "no future");
//
//    match = s->window + cur_match;
//
//    /* Return failure if the match length is less than 2:
//     */
//    if (match[0] != scan[0] || match[1] != scan[1]) return MIN_MATCH-1;
//
//    /* The check at best_len-1 can be removed because it will be made
//     * again later. (This heuristic is not always a win.)
//     * It is not necessary to compare scan[2] and match[2] since they
//     * are always equal when the other bytes match, given that
//     * the hash keys are equal and that HASH_BITS >= 8.
//     */
//    scan += 2, match += 2;
//    Assert(*scan == *match, "match[2]?");
//
//    /* We check for insufficient lookahead only every 8th comparison;
//     * the 256th check will be made at strstart+258.
//     */
//    do {
//    } while (*++scan == *++match && *++scan == *++match &&
//             *++scan == *++match && *++scan == *++match &&
//             *++scan == *++match && *++scan == *++match &&
//             *++scan == *++match && *++scan == *++match &&
//             scan < strend);
//
//    Assert(scan <= s->window+(unsigned)(s->window_size-1), "wild scan");
//
//    len = MAX_MATCH - (int)(strend - scan);
//
//    if (len < MIN_MATCH) return MIN_MATCH - 1;
//
//    s->match_start = cur_match;
//    return (uint)len <= s->lookahead ? (uint)len : s->lookahead;
//}
//
//#endif /* FASTEST */
//
//#ifdef ZLIB_DEBUG
//
//#define EQUAL 0
///* result of memcmp for equal strings */
//
///* ===========================================================================
// * Check that the match at match_start is indeed a match.
// */
//local void check_match(s, start, match, length)
//    deflate_state *s;
//    IPos start, match;
//    int length;
//{
//    /* check that the match is indeed a match */
//    if (zmemcmp(s->window + match,
//                s->window + start, length) != EQUAL) {
//        fprintf(stderr, " start %u, match %u, length %d\n",
//                start, match, length);
//        do {
//            fprintf(stderr, "%c%c", s->window[match++], s->window[start++]);
//        } while (--length != 0);
//        z_error("invalid match");
//    }
//    if (z_verbose > 1) {
//        fprintf(stderr,"\\[%d,%d]", start-match, length);
//        do { putc(s->window[start++], stderr); } while (--length != 0);
//    }
//}
//#else
private static void check_match(deflate_state s, uint start, uint match, uint length) { }
//#endif /* ZLIB_DEBUG */

/* ===========================================================================
 * Fill the window when the lookahead becomes insufficient.
 * Updates strstart and lookahead.
 *
 * IN assertion: lookahead < MIN_LOOKAHEAD
 * OUT assertions: strstart <= window_size-MIN_LOOKAHEAD
 *    At least one byte has been read, or avail_in == 0; reads are
 *    performed for at least two bytes (required for the zip translate_eol
 *    option -- not supported here).
 */
private static void fill_window(deflate_state s) {
    uint n;
    uint more;    /* Amount of free space at the end of the window. */
    uint wsize = s.w_size;

    //Assert(s.lookahead < MIN_LOOKAHEAD, "already enough lookahead");

    do {
        more = (uint)(s.window_size -(ulong)s.lookahead -(ulong)s.strstart);

        /* Deal with !@#$% 64K limit: */
        //if (sizeof(int) <= 2) {
        //    if (more == 0 && s.strstart == 0 && s.lookahead == 0) {
        //        more = wsize;
        //
        //    } else if (more == uint.MaxValue) {
        //        /* Very unlikely, but possible on 16 bit machine if
        //         * strstart == 0 && lookahead == 1 (input done a byte at time)
        //         */
        //        more--;
        //    }
        //}

        /* If the window is almost full and there is insufficient lookahead,
         * move the upper half to the lower one to make room in the upper half.
         */
        if (s.strstart >= wsize+MAX_DIST(s)) {

            zutil.zmemcpy(s.window_array, s.window_index, s.window_array, s.window_index + (long)wsize, (uint)wsize - more);
            s.match_start -= wsize;
            s.strstart    -= wsize; /* we now have strstart >= MAX_DIST */
            s.block_start -= (long) wsize;
            slide_hash(s);
            more += wsize;
        }
        if (s.strm.avail_in == 0) break;

        /* If there was no sliding:
         *    strstart <= WSIZE+MAX_DIST-1 && lookahead <= MIN_LOOKAHEAD - 1 &&
         *    more == window_size - lookahead - strstart
         * => more >= window_size - (MIN_LOOKAHEAD-1 + WSIZE + MAX_DIST-1)
         * => more >= window_size - 2*WSIZE + 2
         * In the BIG_MEM or MMAP case (not yet supported),
         *   window_size == input_size + MIN_LOOKAHEAD  &&
         *   strstart + s.lookahead <= input_size => more >= MIN_LOOKAHEAD.
         * Otherwise, window_size == 2*WSIZE so more >= 2.
         * If there was sliding, more >= WSIZE. So in all cases, more >= 2.
         */
        //Assert(more >= 2, "more < 2");

        n = read_buf(s.strm, s.window_array, s.window_index + s.strstart + s.lookahead, more);
        s.lookahead += n;

        /* Initialize the hash value now that we have some input: */
        if (s.lookahead + s.insert >= zutil.MIN_MATCH) {
            uint str = s.strstart - s.insert;
            s.ins_h = s.window_array[s.window_index + str];
            UPDATE_HASH(s, ref s.ins_h, s.window_array[s.window_index + str + 1]);
//#if MIN_MATCH != 3
//            Call UPDATE_HASH() MIN_MATCH-3 more times
//#endif
            while (s.insert != 0) {
                UPDATE_HASH(s, ref s.ins_h, s.window_array[s.window_index + str + zutil.MIN_MATCH-1]);
//#ifndef FASTEST
                s.prev_array[s.prev_index + (str & s.w_mask)] = s.head_array[s.head_index + s.ins_h];
//#endif
                s.head_array[s.head_index + s.ins_h] = (ushort)str;
                str++;
                s.insert--;
                if (s.lookahead + s.insert < zutil.MIN_MATCH)
                    break;
            }
        }
        /* If the whole input has less than MIN_MATCH bytes, ins_h is garbage,
         * but this is not important since only literal bytes will be emitted.
         */

    } while (s.lookahead < MIN_LOOKAHEAD && s.strm.avail_in != 0);

    /* If the WIN_INIT bytes after the end of the current data have never been
     * written, then zero those bytes in order to avoid memory check reports of
     * the use of uninitialized (or uninitialised as Julian writes) bytes by
     * the longest match routines.  Update the high water mark for the next
     * time through here.  WIN_INIT is set to MAX_MATCH since the longest match
     * routines allow scanning to strstart + MAX_MATCH, ignoring lookahead.
     */
    if (s.high_water < s.window_size) {
        ulong curr = s.strstart + (ulong)(s.lookahead);
        ulong init;

        if (s.high_water < curr) {
            /* Previous high water mark below current data -- zero WIN_INIT
             * bytes or up to end of window, whichever is less.
             */
            init = s.window_size - curr;
            if (init > WIN_INIT)
                init = WIN_INIT;
            zutil.zmemzero(s.window_array, s.window_index + (long)curr, (uint)init);
            s.high_water = curr + init;
        }
        else if (s.high_water < (ulong)curr + WIN_INIT) {
            /* High water mark at or above current data, but below current data
             * plus WIN_INIT -- zero out to current data plus WIN_INIT, or up
             * to end of window, whichever is less.
             */
            init = (ulong)curr + WIN_INIT - s.high_water;
            if (init > s.window_size - s.high_water)
                init = s.window_size - s.high_water;
            zutil.zmemzero(s.window_array, s.window_index + (long)s.high_water, (uint)init);
            s.high_water += init;
        }
    }

    //Assert((ulong)s.strstart <= s.window_size - MIN_LOOKAHEAD,
    //       "not enough room for search");
}

/* ===========================================================================
 * Flush the current block, with given end-of-file flag.
 * IN assertion: strstart is set to the end of the current match.
 */
private static void FLUSH_BLOCK_ONLY(deflate_state s, int last) {
   byte[] array;
   long index;
   if (s.block_start >= 0L) {
      array = s.window_array;
      index = s.window_index + s.block_start;
   } else {
      array = null;
      index = 0;
   }

   trees._tr_flush_block(s, array, index,
                (ulong)((long)s.strstart - s.block_start),
                (last));
   s.block_start = s.strstart;
   flush_pending(s.strm);
   //Tracev((stderr,"[FLUSH]"));
}

/* Same but force premature exit if necessary. */
private static block_state FLUSH_BLOCK_(deflate_state s, int last) {
   FLUSH_BLOCK_ONLY(s, last);
   if (s.strm.avail_out == 0) return (last != 0) ? block_state.finish_started : block_state.need_more;
   return block_state._continue;
}

/* Maximum stored block length in deflate format (not including header). */
private const uint MAX_STORED = 65535;

/* Minimum of a and b. */
private static ulong MIN(ulong a, ulong b) { return ((a) > (b) ? (b) : (a)); }
private static uint MIN(uint a, uint b) { return ((a) > (b) ? (b) : (a)); }

/* ===========================================================================
 * Copy without compression as much as possible from the input stream, return
 * the current block state.
 *
 * In case deflateParams() is used to later switch to a non-zero compression
 * level, s->matches (otherwise unused when storing) keeps track of the number
 * of hash table slides to perform. If s->matches is 1, then one hash table
 * slide will be done when switching. If s->matches is 2, the maximum value
 * allowed here, then the hash table will be cleared, since two or more slides
 * is the same as a clear.
 *
 * deflate_stored() is written to minimize the number of times an input byte is
 * copied. It is most efficient with large input and output buffers, which
 * maximizes the opportunites to have a single copy from next_in to next_out.
 */
private static block_state deflate_stored(deflate_state s, int flush) {
    /* Smallest worthy block size when not flushing or finishing. By default
     * this is 32K. This can be as small as 507 bytes for memLevel == 1. For
     * large input and output buffers, the stored block size will be larger.
     */
    uint min_block = (uint)(MIN(s.pending_buf_size - 5, (ulong)s.w_size));

    /* Copy as many min_block or larger stored blocks directly to next_out as
     * possible. If flushing, copy the remaining available input to next_out as
     * stored blocks, if there is enough space.
     */
    uint len, left, have, last = 0;
    uint used = s.strm.avail_in;
    do {
        /* Set len to the maximum size block that we can copy directly with the
         * available input data and output space. Set left to how much of that
         * would be copied from what's left in the window.
         */
        len = MAX_STORED;       /* maximum deflate stored block length */
        have = (uint)((s.bi_valid + 42) >> 3);         /* number of header bytes */
        if (s.strm.avail_out < have)          /* need room for header */
            break;
            /* maximum stored block length that will fit in avail_out: */
        have = s.strm.avail_out - have;
        left = (uint)(((long)s.strstart) - s.block_start);    /* bytes left in window */
        if (len > (ulong)left + s.strm.avail_in)
            len = left + s.strm.avail_in;     /* limit len to the input */
        if (len > have)
            len = have;                         /* limit len to the output */

        /* If the stored block would be less than min_block in length, or if
         * unable to copy all of the available input when flushing, then try
         * copying to the window and the pending buffer instead. Also don't
         * write an empty block when flushing -- deflate() does that.
         */
        if (len < min_block && ((len == 0 && flush != zlib.Z_FINISH) ||
                                flush == zlib.Z_NO_FLUSH ||
                                len != left + s.strm.avail_in))
            break;

        /* Make a dummy stored block in pending to get the header bytes,
         * including any pending bits. This also updates the debugging counts.
         */
        last = (flush == zlib.Z_FINISH) && (len == left + s.strm.avail_in) ? 1u : 0u;
        trees._tr_stored_block(s, null, 0, 0L, (int)last);

        /* Replace the lengths in the dummy stored block with len. */
        s.pending_buf[s.pending - 4] = (byte)(len);
        s.pending_buf[s.pending - 3] = (byte)(len >> 8);
        s.pending_buf[s.pending - 2] = (byte)(~len);
        s.pending_buf[s.pending - 1] = (byte)(~len >> 8);

        /* Write the stored block header bytes. */
        flush_pending(s.strm);

//#ifdef ZLIB_DEBUG
//        /* Update debugging counts for the data about to be copied. */
//        s.compressed_len += len << 3;
//        s.bits_sent += len << 3;
//#endif

        /* Copy uncompressed bytes from the window to next_out. */
        if (left != 0) {
            if (left > len)
                left = len;
            zutil.zmemcpy(s.strm.output_buffer, s.strm.next_out, s.window_array, s.window_index + s.block_start, left);
            s.strm.next_out += left;
            s.strm.avail_out -= left;
            s.strm.total_out += left;
            s.block_start += left;
            len -= left;
        }

        /* Copy uncompressed bytes directly from next_in to next_out, updating
         * the check value.
         */
        if (len != 0) {
            read_buf(s.strm, s.strm.output_buffer, s.strm.next_out, len);
            s.strm.next_out += len;
            s.strm.avail_out -= len;
            s.strm.total_out += len;
        }
    } while (last == 0);

    /* Update the sliding window with the last s.w_size bytes of the copied
     * data, or append all of the copied data to the existing window if less
     * than s.w_size bytes were copied. Also update the number of bytes to
     * insert in the hash tables, in the event that deflateParams() switches to
     * a non-zero compression level.
     */
    used -= s.strm.avail_in;      /* number of input bytes directly copied */
    if (used != 0) {
        /* If any input was used, then no unused input remains in the window,
         * therefore s.block_start == s.strstart.
         */
        if (used >= s.w_size) {    /* supplant the previous history */
            s.matches = 2;         /* clear hash */
            zutil.zmemcpy(s.window_array, s.window_index, s.strm.input_buffer, s.strm.next_in - s.w_size, s.w_size);
            s.strstart = s.w_size;
        }
        else {
            if (s.window_size - s.strstart <= used) {
                /* Slide the window down. */
                s.strstart -= s.w_size;
                zutil.zmemcpy(s.window_array, s.window_index, s.window_array, s.window_index + s.w_size, s.strstart);
                if (s.matches < 2)
                    s.matches++;   /* add a pending slide_hash() */
            }
            zutil.zmemcpy(s.window_array, s.window_index + s.strstart, s.strm.input_buffer, s.strm.next_in - used, used);
            s.strstart += used;
        }
        s.block_start = s.strstart;
        s.insert += MIN(used, s.w_size - s.insert);
    }
    if (s.high_water < s.strstart)
        s.high_water = s.strstart;

    /* If the last block was written to next_out, then done. */
    if (last != 0)
        return block_state.finish_done;

    /* If flushing and all input has been consumed, then done. */
    if (flush != zlib.Z_NO_FLUSH && flush != zlib.Z_FINISH &&
        s.strm.avail_in == 0 && (long)s.strstart == s.block_start)
        return block_state.block_done;

    /* Fill the window with any remaining input. */
    have = (uint)(s.window_size - s.strstart - 1);
    if (s.strm.avail_in > have && s.block_start >= (long)s.w_size) {
        /* Slide the window down. */
        s.block_start -= s.w_size;
        s.strstart -= s.w_size;
        zutil.zmemcpy(s.window_array, s.window_index, s.window_array, s.window_index + s.w_size, s.strstart);
        if (s.matches < 2)
            s.matches++;           /* add a pending slide_hash() */
        have += s.w_size;          /* more space now */
    }
    if (have > s.strm.avail_in)
        have = s.strm.avail_in;
    if (have != 0) {
        read_buf(s.strm, s.window_array, s.window_index + s.strstart, have);
        s.strstart += have;
    }
    if (s.high_water < s.strstart)
        s.high_water = s.strstart;

    /* There was not enough avail_out to write a complete worthy or flushed
     * stored block to next_out. Write a stored block to pending instead, if we
     * have enough input for a worthy block, or if flushing and there is enough
     * room for the remaining input as a stored block in the pending buffer.
     */
    have = (uint)((s.bi_valid + 42) >> 3);         /* number of header bytes */
        /* maximum stored block length that will fit in pending: */
    have = (uint)MIN(s.pending_buf_size - have, MAX_STORED);
    min_block = MIN(have, s.w_size);
    left = (uint)(((long)s.strstart) - s.block_start);
    if (left >= min_block ||
        ((left != 0 || flush == zlib.Z_FINISH) && flush != zlib.Z_NO_FLUSH &&
         s.strm.avail_in == 0 && left <= have)) {
        len = MIN(left, have);
        last = flush == zlib.Z_FINISH && s.strm.avail_in == 0 &&
               len == left ? 1u : 0u;
        trees._tr_stored_block(s, s.window_array, s.window_index + s.block_start, len, (int)last);
        s.block_start += len;
        flush_pending(s.strm);
    }

    /* We've done all we can with the available input and output. */
    return last != 0 ? block_state.finish_started : block_state.need_more;
}

/* ===========================================================================
 * Compress as much as possible from the input stream, return the current
 * block state.
 * This function does not perform lazy evaluation of matches and inserts
 * new strings in the dictionary only for unmatched strings or for short
 * matches. It is used only for the fast compression options.
 */
private static block_state deflate_fast(deflate_state s, int flush) {
    uint hash_head;       /* head of the hash chain */
    int bflush;           /* set if current block must be flushed */

    for (;;) {
        /* Make sure that we always have enough lookahead, except
         * at the end of the input file. We need MAX_MATCH bytes
         * for the next match, plus MIN_MATCH bytes to insert the
         * string following the next match.
         */
        if (s.lookahead < MIN_LOOKAHEAD) {
            fill_window(s);
            if (s.lookahead < MIN_LOOKAHEAD && flush == zlib.Z_NO_FLUSH) {
                return block_state.need_more;
            }
            if (s.lookahead == 0) break; /* flush the current block */
        }

        /* Insert the string window[strstart .. strstart+2] in the
         * dictionary, and set hash_head to the head of the hash chain:
         */
        hash_head = NIL;
        if (s.lookahead >= zutil.MIN_MATCH) {
            INSERT_STRING(s, s.strstart, ref hash_head);
        }

        /* Find the longest match, discarding those <= prev_length.
         * At this point we have always match_length < MIN_MATCH
         */
        if (hash_head != NIL && s.strstart - hash_head <= MAX_DIST(s)) {
            /* To simplify the code, we prevent matches with the string
             * of window index 0 (in particular we have to avoid a match
             * of the string with itself at the start of the input file).
             */
            s.match_length = longest_match (s, hash_head);
            /* longest_match() sets match_start */
        }
        if (s.match_length >= zutil.MIN_MATCH) {
            check_match(s, s.strstart, s.match_start, s.match_length);

            _tr_tally_dist(s, s.strstart - s.match_start,
                           s.match_length - zutil.MIN_MATCH, out bflush);

            s.lookahead -= s.match_length;

            /* Insert new strings in the hash table only if the match length
             * is not too large. This saves time but degrades compression.
             */
//#ifndef FASTEST
            if (s.match_length <= s.max_lazy_match &&
                s.lookahead >= zutil.MIN_MATCH) {
                s.match_length--; /* string at strstart already in table */
                do {
                    s.strstart++;
                    INSERT_STRING(s, s.strstart, ref hash_head);
                    /* strstart never exceeds WSIZE-MAX_MATCH, so there are
                     * always MIN_MATCH bytes ahead.
                     */
                } while (--s.match_length != 0);
                s.strstart++;
            } else
//#endif
            {
                s.strstart += s.match_length;
                s.match_length = 0;
                s.ins_h = s.window_array[s.window_index + s.strstart];
                UPDATE_HASH(s, ref s.ins_h, s.window_array[s.window_index + s.strstart+1]);
//#if MIN_MATCH != 3
//                Call UPDATE_HASH() MIN_MATCH-3 more times
//#endif
                /* If lookahead < MIN_MATCH, ins_h is garbage, but it does not
                 * matter since it will be recomputed at next deflate call.
                 */
            }
        } else {
            /* No match, output a literal byte */
            //Tracevv((stderr,"%c", s.window[s.strstart]));
            _tr_tally_lit (s, s.window_array[s.window_index + s.strstart], out bflush);
            s.lookahead--;
            s.strstart++;
        }
        if (bflush != 0) {
            var res = FLUSH_BLOCK_(s, 0);
            if (res != block_state._continue) return res;
        }
    }
    s.insert = s.strstart < zutil.MIN_MATCH -1 ? s.strstart : zutil.MIN_MATCH -1;
    if (flush == zlib.Z_FINISH) {
        var res = FLUSH_BLOCK_(s, 1);
        if (res != block_state._continue) return res;
        return block_state.finish_done;
    }
    if (s.last_lit != 0) {
        var res = FLUSH_BLOCK_(s, 0);
        if (res != block_state._continue) return res;
    }
    return block_state.block_done;
}

//#ifndef FASTEST
/* ===========================================================================
 * Same as above, but achieves better compression. We use a lazy
 * evaluation for matches: a match is finally adopted only if there is
 * no better match at the next window position.
 */
private static block_state deflate_slow(deflate_state s, int flush) {
    uint hash_head;          /* head of hash chain */
    int bflush;              /* set if current block must be flushed */

    /* Process the input block. */
    for (;;) {
        /* Make sure that we always have enough lookahead, except
         * at the end of the input file. We need MAX_MATCH bytes
         * for the next match, plus MIN_MATCH bytes to insert the
         * string following the next match.
         */
        if (s.lookahead < MIN_LOOKAHEAD) {
            fill_window(s);
            if (s.lookahead < MIN_LOOKAHEAD && flush == zlib.Z_NO_FLUSH) {
                return block_state.need_more;
            }
            if (s.lookahead == 0) break; /* flush the current block */
        }

        /* Insert the string window[strstart .. strstart+2] in the
         * dictionary, and set hash_head to the head of the hash chain:
         */
        hash_head = NIL;
        if (s.lookahead >= zutil.MIN_MATCH) {
            INSERT_STRING(s, s.strstart, ref hash_head);
        }

        /* Find the longest match, discarding those <= prev_length.
         */
        s.prev_length = s.match_length; s.prev_match = s.match_start;
        s.match_length = zutil.MIN_MATCH -1;

        if (hash_head != NIL && s.prev_length < s.max_lazy_match &&
            s.strstart - hash_head <= MAX_DIST(s)) {
            /* To simplify the code, we prevent matches with the string
             * of window index 0 (in particular we have to avoid a match
             * of the string with itself at the start of the input file).
             */
            s.match_length = longest_match (s, hash_head);
            /* longest_match() sets match_start */

            if (s.match_length <= 5 && (s.strategy == zlib.Z_FILTERED
//#if TOO_FAR <= 32767
                || (s.match_length == zutil.MIN_MATCH &&
                    s.strstart - s.match_start > TOO_FAR)
//#endif
                )) {

                /* If prev_match is also MIN_MATCH, match_start is garbage
                 * but we will ignore the current match anyway.
                 */
                s.match_length = zutil.MIN_MATCH -1;
            }
        }
        /* If there was a match at the previous step and the current
         * match is not better, output the previous match:
         */
        if (s.prev_length >= zutil.MIN_MATCH && s.match_length <= s.prev_length) {
            uint max_insert = s.strstart + s.lookahead - zutil.MIN_MATCH;
            /* Do not insert strings in hash table beyond this. */

            check_match(s, s.strstart-1, s.prev_match, s.prev_length);

            _tr_tally_dist(s, s.strstart -1 - s.prev_match,
                           s.prev_length - zutil.MIN_MATCH, out bflush);

            /* Insert in hash table all strings up to the end of the match.
             * strstart-1 and strstart are already inserted. If there is not
             * enough lookahead, the last two strings are not inserted in
             * the hash table.
             */
            s.lookahead -= s.prev_length-1;
            s.prev_length -= 2;
            do {
                if (++s.strstart <= max_insert) {
                    INSERT_STRING(s, s.strstart, ref hash_head);
                }
            } while (--s.prev_length != 0);
            s.match_available = 0;
            s.match_length = zutil.MIN_MATCH -1;
            s.strstart++;

            if (bflush != 0) {
                var res = FLUSH_BLOCK_(s, 0);
                if (res != block_state._continue) return res;
            }

        } else if (s.match_available != 0) {
            /* If there was no match at the previous position, output a
             * single literal. If there was a match but the current match
             * is longer, truncate the previous match to a single literal.
             */
            //Tracevv((stderr,"%c", s.window[s.strstart-1]));
            _tr_tally_lit(s, s.window_array[s.window_index + s.strstart-1], out bflush);
            if (bflush != 0) {
                FLUSH_BLOCK_ONLY(s, 0);
            }
            s.strstart++;
            s.lookahead--;
            if (s.strm.avail_out == 0) return block_state.need_more;
        } else {
            /* There is no previous match to compare with, wait for
             * the next step to decide.
             */
            s.match_available = 1;
            s.strstart++;
            s.lookahead--;
        }
    }
    //Assert (flush != Z_NO_FLUSH, "no flush?");
    if (s.match_available != 0) {
        //Tracevv((stderr,"%c", s.window[s.strstart-1]));
        _tr_tally_lit(s, s.window_array[s.window_index + s.strstart-1], out bflush);
        s.match_available = 0;
    }
    s.insert = s.strstart < zutil.MIN_MATCH -1 ? s.strstart : zutil.MIN_MATCH -1;
    if (flush == zlib.Z_FINISH) {
        var res = FLUSH_BLOCK_(s, 1);
        if (res != block_state._continue) return res;
        return block_state.finish_done;
    }
    if (s.last_lit != 0) {
        var res = FLUSH_BLOCK_(s, 0);
        if (res != block_state._continue) return res;
    }
    return block_state.block_done;
}
//#endif /* FASTEST */

/* ===========================================================================
 * For Z_RLE, simply look for runs of bytes, generate matches only of distance
 * one.  Do not maintain a hash table.  (It will be regenerated if this run of
 * deflate switches away from Z_RLE.)
 */
private static block_state deflate_rle(deflate_state s, int flush) {
    int bflush;             /* set if current block must be flushed */
    uint prev;              /* byte at distance one to match */
    byte[] scan_array;
    long scan_index;
    long strend_index;   /* scan goes up to strend for length of run */

    for (;;) {
        /* Make sure that we always have enough lookahead, except
         * at the end of the input file. We need MAX_MATCH bytes
         * for the longest run, plus one for the unrolled loop.
         */
        if (s.lookahead <= zutil.MAX_MATCH) {
            fill_window(s);
            if (s.lookahead <= zutil.MAX_MATCH && flush == zlib.Z_NO_FLUSH) {
                return block_state.need_more;
            }
            if (s.lookahead == 0) break; /* flush the current block */
        }

        /* See how many times the previous byte repeats */
        s.match_length = 0;
        if (s.lookahead >= zutil.MIN_MATCH && s.strstart > 0) {
            scan_array = s.window_array;
            scan_index = s.window_index + s.strstart - 1;
            prev = scan_array[scan_index];
            if (prev == scan_array[++scan_index] && prev == scan_array[++scan_index] && prev == scan_array[++scan_index]) {
                strend_index = s.window_index + s.strstart + zutil.MAX_MATCH;
                do {
                } while (prev == scan_array[++scan_index] && prev == scan_array[++scan_index] &&
                         prev == scan_array[++scan_index] && prev == scan_array[++scan_index] &&
                         prev == scan_array[++scan_index] && prev == scan_array[++scan_index] &&
                         prev == scan_array[++scan_index] && prev == scan_array[++scan_index] &&
                         scan_index < strend_index);
                s.match_length = zutil.MAX_MATCH - (uint)(strend_index - scan_index);
                if (s.match_length > s.lookahead)
                    s.match_length = s.lookahead;
            }
            //Assert(scan <= s.window+(uint)(s.window_size-1), "wild scan");
        }

        /* Emit match if have run of MIN_MATCH or longer, else emit literal */
        if (s.match_length >= zutil.MIN_MATCH) {
            check_match(s, s.strstart, s.strstart - 1, s.match_length);

            _tr_tally_dist(s, 1, s.match_length - zutil.MIN_MATCH, out bflush);

            s.lookahead -= s.match_length;
            s.strstart += s.match_length;
            s.match_length = 0;
        } else {
            /* No match, output a literal byte */
            //Tracevv((stderr,"%c", s.window[s.strstart]));
            _tr_tally_lit (s, s.window_array[s.window_index + s.strstart], out bflush);
            s.lookahead--;
            s.strstart++;
        }
        if (bflush != 0) {
            var res = FLUSH_BLOCK_(s, 0);
            if (res != block_state._continue) return res;
        }
    }
    s.insert = 0;
    if (flush == zlib.Z_FINISH) {
        var res = FLUSH_BLOCK_(s, 1);
        if (res != block_state._continue) return res;
        return block_state.finish_done;
    }
    if (s.last_lit != 0) {
        var res = FLUSH_BLOCK_(s, 0);
        if (res != block_state._continue) return res;
    }
    return block_state.block_done;
}

///* ===========================================================================
// * For Z_HUFFMAN_ONLY, do not look for matches.  Do not maintain a hash table.
// * (It will be regenerated if this run of deflate switches away from Huffman.)
// */
private static block_state deflate_huff(deflate_state s, int flush) {
    int bflush;             /* set if current block must be flushed */

    for (;;) {
        /* Make sure that we have a literal to write. */
        if (s.lookahead == 0) {
            fill_window(s);
            if (s.lookahead == 0) {
                if (flush == zlib.Z_NO_FLUSH)
                    return block_state.need_more;
                break;      /* flush the current block */
            }
        }

        /* Output a literal byte */
        s.match_length = 0;
        //Tracevv((stderr,"%c", s.window[s.strstart]));
        _tr_tally_lit (s, s.window_array[s.window_index + s.strstart], out bflush);
        s.lookahead--;
        s.strstart++;
        if (bflush != 0) {
            var res = FLUSH_BLOCK_(s, 0);
            if (res != block_state._continue) return res;
        }
    }
    s.insert = 0;
    if (flush == zlib.Z_FINISH) {
        var res = FLUSH_BLOCK_(s, 1);
        if (res != block_state._continue) return res;
        return block_state.finish_done;
    }
    if (s.last_lit != 0) {
        var res = FLUSH_BLOCK_(s, 0);
        if (res != block_state._continue) return res;
    }
    return block_state.block_done;
}
    }
}
