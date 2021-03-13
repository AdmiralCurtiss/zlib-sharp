// port of inffast.c

namespace zlib_sharp {
	public static class inffast {
/* inffast.c -- fast decoding
 * Copyright (C) 1995-2017 Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/*
   Decode literal, length, and distance codes and write out the resulting
   literal and match bytes until either not enough input or output is
   available, an end-of-block is encountered, or a data error is encountered.
   When large enough input and output buffers are supplied to inflate(), for
   example, a 16K input buffer and a 64K output buffer, more than 95% of the
   inflate execution time is spent in this routine.

   Entry assumptions:

        state->mode == LEN
        strm->avail_in >= 6
        strm->avail_out >= 258
        start >= strm->avail_out
        state->bits < 8

   On return, state->mode is one of:

        LEN -- ran out of enough output space or enough available input
        TYPE -- reached end of block code, inflate() to interpret next block
        BAD -- error in block data

   Notes:

    - The maximum input bits used by a length/distance pair is 15 bits for the
      length code, 5 bits for the length extra, 15 bits for the distance code,
      and 13 bits for the distance extra.  This totals 48 bits, or six bytes.
      Therefore if strm->avail_in >= 6, then there is enough input to avoid
      checking for available input while decoding.

    - The maximum bytes that a single length/distance pair can output is 258
      bytes, which is the maximum length that can be coded.  inflate_fast()
      requires strm->avail_out >= 258 for each loop to avoid checking for
      output space.
 */
public static void inflate_fast(
z_stream strm,
uint start)         /* inflate()'s starting value for strm->avail_out */
{
    inflate_state state;
    byte[] in_array;
    long in_index;      /* local strm->next_in */
    long last;    /* have enough input while in < last */
    byte [] out_array;     /* local strm->next_out */
    long out_index;
    long beg;     /* inflate()'s initial strm->next_out */
    long end;     /* while out < end, enough space available */
//#ifdef INFLATE_STRICT
//    unsigned dmax;              /* maximum distance from zlib header */
//#endif
    uint wsize;             /* window size or zero if not using window */
    uint whave;             /* valid bytes in the window */
    uint wnext;             /* window write index */
    byte[] window;  /* allocated sliding window, if wsize != 0 */
    ulong hold;         /* local strm->hold */
    uint bits;              /* local strm->bits */
    code[] lcode_array;      /* local strm->lencode */
    long lcode_index;
    code[] dcode_array;      /* local strm->distcode */
    long dcode_index;
    uint lmask;             /* mask for first level of length codes */
    uint dmask;             /* mask for first level of distance codes */
    code here;                  /* retrieved table entry */
    uint op;                /* code bits, operation, extra bits, or */
                                /*  window position, window bytes to copy */
    uint len;               /* match length, unused bytes */
    uint dist;              /* match distance */
    byte[] from_array;    /* where to copy match from */
    long from_index;

    /* copy state to local variables */
    state = strm.state;
    in_array = strm.input_buffer;
    in_index = strm.next_in;
    last = in_index + (strm.avail_in - 5);
    out_array = strm.output_buffer;
    out_index = strm.next_out;
    beg = out_index - (start - strm.avail_out);
    end = out_index + (strm.avail_out - 257);
//#ifdef INFLATE_STRICT
//    dmax = state->dmax;
//#endif
    wsize = state.wsize;
    whave = state.whave;
    wnext = state.wnext;
    window = state.window;
    hold = state.hold;
    bits = state.bits;
    lcode_array = state.lencode_array;
    lcode_index = state.lencode_index;
    dcode_array = state.distcode_array;
    dcode_index = state.distcode_index;
    lmask = (1U << (int)state.lenbits) - 1;
    dmask = (1U << (int)state.distbits) - 1;

    /* decode literals and length/distances until end-of-block or not enough
       input data or output space */
    do {
        if (bits < 15) {
            hold += (ulong)(in_array[in_index++]) << (int)bits;
            bits += 8;
            hold += (ulong)(in_array[in_index++]) << (int)bits;
            bits += 8;
        }
        here = lcode_array[lcode_index + (long)(hold & lmask)];
      dolen:
        op = (uint)(here.bits);
        hold >>= (int)op;
        bits -= op;
        op = (uint)(here.op);
        if (op == 0) {                          /* literal */
            //Tracevv((stderr, here.val >= 0x20 && here.val < 0x7f ?
            //        "inflate:         literal '%c'\n" :
            //        "inflate:         literal 0x%02x\n", here.val));
            out_array[out_index++] = (byte)(here.val);
        }
        else if ((op & 16) != 0) {                     /* length base */
            len = (uint)(here.val);
            op &= 15;                           /* number of extra bits */
            if (op != 0) {
                if (bits < op) {
                    hold += (ulong)(in_array[in_index++]) << (int)bits;
                    bits += 8;
                }
                len += (uint)hold & ((1U << (int)op) - 1);
                hold >>= (int)op;
                bits -= op;
            }
            //Tracevv((stderr, "inflate:         length %u\n", len));
            if (bits < 15) {
                hold += (ulong)(in_array[in_index++]) << (int)bits;
                bits += 8;
                hold += (ulong)(in_array[in_index++]) << (int)bits;
                bits += 8;
            }
            here = dcode_array[dcode_index + (long)(hold & dmask)];
          dodist:
            op = (uint)(here.bits);
            hold >>= (int)op;
            bits -= op;
            op = (uint)(here.op);
            if ((op & 16) != 0) {                      /* distance base */
                dist = (uint)(here.val);
                op &= 15;                       /* number of extra bits */
                if (bits < op) {
                    hold += (ulong)(in_array[in_index++]) << (int)bits;
                    bits += 8;
                    if (bits < op) {
                        hold += (ulong)(in_array[in_index++]) << (int)bits;
                        bits += 8;
                    }
                }
                dist += (uint)hold & ((1U << (int)op) - 1);
//#ifdef INFLATE_STRICT
//                if (dist > dmax) {
//                    strm->msg = (char *)"invalid distance too far back";
//                    state->mode = BAD;
//                    break;
//                }
//#endif
                hold >>= (int)op;
                bits -= op;
                //Tracevv((stderr, "inflate:         distance %u\n", dist));
                op = (uint)(out_index - beg);     /* max distance in output */
                if (dist > op) {                /* see if copy from window */
                    op = dist - op;             /* distance back in window */
                    if (op > whave) {
                        if (state.sane != 0) {
                            strm.msg =
                                "invalid distance too far back";
                            state.mode = inflate_mode.BAD;
                            break;
                        }
//#ifdef INFLATE_ALLOW_INVALID_DISTANCE_TOOFAR_ARRR
//                        if (len <= op - whave) {
//                            do {
//                                *out++ = 0;
//                            } while (--len);
//                            continue;
//                        }
//                        len -= op - whave;
//                        do {
//                            *out++ = 0;
//                        } while (--op > whave);
//                        if (op == 0) {
//                            from = out - dist;
//                            do {
//                                *out++ = *from++;
//                            } while (--len);
//                            continue;
//                        }
//#endif
                    }
                    from_array = window;
                    from_index = 0;
                    if (wnext == 0) {           /* very common case */
                        from_index += wsize - op;
                        if (op < len) {         /* some from window */
                            len -= op;
                            do {
                                out_array[out_index++] = from_array[from_index++];
                            } while (--op != 0);
                            from_array = out_array;
                            from_index = out_index - dist;  /* rest from output */
                        }
                    }
                    else if (wnext < op) {      /* wrap around window */
                        from_index += wsize + wnext - op;
                        op -= wnext;
                        if (op < len) {         /* some from end of window */
                            len -= op;
                            do {
                                out_array[out_index++] = from_array[from_index++];
                            } while (--op != 0);
                            from_array = window;
                            from_index = 0;
                            if (wnext < len) {  /* some from start of window */
                                op = wnext;
                                len -= op;
                                do {
                                    out_array[out_index++] = from_array[from_index++];
                                } while (--op != 0);
                                from_array = out_array;
                                from_index = out_index - dist;  /* rest from output */
                            }
                        }
                    }
                    else {                      /* contiguous in window */
                        from_index += wnext - op;
                        if (op < len) {         /* some from window */
                            len -= op;
                            do {
                                out_array[out_index++] = from_array[from_index++];
                            } while (--op != 0);
                            from_array = out_array;
                            from_index = out_index - dist;  /* rest from output */
                        }
                    }
                    while (len > 2) {
                        out_array[out_index++] = from_array[from_index++];
                        out_array[out_index++] = from_array[from_index++];
                        out_array[out_index++] = from_array[from_index++];
                        len -= 3;
                    }
                    if (len != 0) {
                        out_array[out_index++] = from_array[from_index++];
                        if (len > 1)
                            out_array[out_index++] = from_array[from_index++];
                    }
                }
                else {
                    from_array = out_array;
                    from_index = out_index - dist;  /* copy direct from output */
                    do {                        /* minimum length is three */
                        out_array[out_index++] = from_array[from_index++];
                        out_array[out_index++] = from_array[from_index++];
                        out_array[out_index++] = from_array[from_index++];
                        len -= 3;
                    } while (len > 2);
                    if (len != 0) {
                        out_array[out_index++] = from_array[from_index++];
                        if (len > 1)
                            out_array[out_index++] = from_array[from_index++];
                    }
                }
            }
            else if ((op & 64) == 0) {          /* 2nd level distance code */
                here = dcode_array[dcode_index + here.val + (long)(hold & ((1U << (int)op) - 1))];
                goto dodist;
            }
            else {
                strm.msg = "invalid distance code";
                state.mode = inflate_mode.BAD;
                break;
            }
        }
        else if ((op & 64) == 0) {              /* 2nd level length code */
            here = lcode_array[lcode_index + here.val + (long)(hold & ((1U << (int)op) - 1))];
            goto dolen;
        }
        else if ((op & 32) != 0) {                     /* end-of-block */
            //Tracevv((stderr, "inflate:         end of block\n"));
            state.mode = inflate_mode.TYPE;
            break;
        }
        else {
            strm.msg = "invalid literal/length code";
            state.mode = inflate_mode.BAD;
            break;
        }
    } while (in_index < last && out_index < end);

    /* return unused bytes (on entry, bits < 8, so in won't go too far back) */
    len = bits >> 3;
    in_index -= len;
    bits -= len << 3;
    hold &= (1U << (int)bits) - 1;

    /* update state and return */
    strm.next_in = in_index;
    strm.next_out = out_index;
    strm.avail_in = (uint)(in_index < last ? 5 + (last - in_index) : 5 - (in_index - last));
    strm.avail_out = (uint)(out_index < end ?
                                 257 + (end - out_index) : 257 - (out_index - end));
    state.hold = hold;
    state.bits = bits;
    return;
}

/*
   inflate_fast() speedups that turned out slower (on a PowerPC G3 750CXe):
   - Using bit fields for code structure
   - Different op definition to avoid & for extra bits (do & for table bits)
   - Three separate decoding do-loops for direct, window, and wnext == 0
   - Special case for distance > 1 copies to do overlapped load and store copy
   - Explicit branch predictions (based on measured branch probabilities)
   - Deferring match copy and interspersed it with decoding subsequent codes
   - Swapping literal/length else
   - Swapping window/direct else
   - Larger unrolled copy loops (three is about right)
   - Moving len -= 3 statement into middle of loop
 */
	}
}
