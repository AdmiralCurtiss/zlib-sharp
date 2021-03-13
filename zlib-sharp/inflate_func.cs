// port of inflate.c's big inflate() function

namespace zlib_sharp {
    public partial class inflate {
/* Macros for inflate(): */

/* check function to use adler32() for zlib or crc32() for gzip */
//#ifdef GUNZIP
private ulong UPDATE(ulong check, byte[] buf_array, long buf_index, uint len) {
    return (state.flags != 0 ? crc32.crc32_(check, buf_array, buf_index, len) : adler32.adler32_(check, buf_array, buf_index, len));
}
//#else
//#  define UPDATE(check, buf, len) adler32(check, buf, len)
//#endif

/* check macros for header crc */
//#ifdef GUNZIP
private void CRC2(ulong check, ulong word) {
    do {
        hbuf[0] = (byte)(word);
        hbuf[1] = (byte)((word) >> 8);
        check = crc32.crc32_(check, hbuf, 0, 2);
    } while (false);
}

private void CRC4(ulong check, ulong word) {
    do {
        hbuf[0] = (byte)(word);
        hbuf[1] = (byte)((word) >> 8);
        hbuf[2] = (byte)((word) >> 16);
        hbuf[3] = (byte)((word) >> 24);
        check = crc32.crc32_(check, hbuf, 0, 4);
    } while (false);
}
//#endif

/* Load registers with state in inflate() for speed */
private void LOAD() {
    do {
        put_index = strm.next_out;
        put_buffer = strm.output_buffer;
        left = strm.avail_out;
        next_index = strm.next_in;
        next_buffer = strm.input_buffer;
        have = strm.avail_in;
        hold = state.hold;
        bits = state.bits;
    } while (false);
}

/* Restore state from registers in inflate() */
private void RESTORE() {
    do {
        strm.next_out = put_index;
        strm.output_buffer = put_buffer;
        strm.avail_out = left;
        strm.next_in = next_index;
        strm.input_buffer = next_buffer;
        strm.avail_in = have;
        state.hold = hold;
        state.bits = bits;
    } while (false);
}

/* Clear the input bit accumulator */
private void INITBITS() {
    do {
        hold = 0;
        bits = 0;
    } while (false);
}

/* Get a byte of input into the bit accumulator, or return from inflate()
   if there is no input available. */
private bool PULLBYTE_() {
    do {
        if (have == 0) return true;
        have--;
        hold += (ulong)(next_buffer[next_index++]) << (int)bits;
        bits += 8;
        return false;
    } while (false);
}

/* Assure that there are at least n bits in the bit accumulator.  If there is
   not enough available input to do that, then return from inflate(). */
private bool NEEDBITS_(long n) {
    do {
        while (bits < (uint)(n)) {
            if (PULLBYTE_())
                return true;
        }
        return false;
    } while (false);
}

/* Return the low n bits of the bit accumulator (n < 16) */
private uint BITS(long n) {
    return ((uint)hold & ((1U << ((int)n)) - 1));
}

/* Remove n bits from the bit accumulator */
private void DROPBITS(long n) {
    do {
        hold >>= ((int)n);
        bits -= (uint)(n);
    } while (false);
}

/* Remove zero to seven bits as needed to go to a byte boundary */
private void BYTEBITS() {
    do {
        hold >>= (int)(bits & 7);
        bits -= bits & 7;
    } while (false);
}

/*
   inflate() uses a state machine to process as much input data and generate as
   much output data as possible before returning.  The state machine is
   structured roughly as follows:

    for (;;) switch (state) {
    ...
    case STATEn:
        if (not enough input data or output space to make progress)
            return;
        ... make progress ...
        state = STATEm;
        break;
    ...
    }

   so when inflate() is called again, the same case is attempted again, and
   if the appropriate resources are provided, the machine proceeds to the
   next state.  The NEEDBITS() macro is usually the way the state evaluates
   whether it can proceed or should return.  NEEDBITS() does the return if
   the requested bits are not available.  The typical use of the BITS macros
   is:

        NEEDBITS(n);
        ... do something with BITS(n) ...
        DROPBITS(n);

   where NEEDBITS(n) either returns from inflate() if there isn't enough
   input left to load n bits into the accumulator, or it continues.  BITS(n)
   gives the low n bits in the accumulator.  When done, DROPBITS(n) drops
   the low n bits off the accumulator.  INITBITS() clears the accumulator
   and sets the number of available bits to zero.  BYTEBITS() discards just
   enough bits to put the accumulator on a byte boundary.  After BYTEBITS()
   and a NEEDBITS(8), then BITS(8) would return the next byte in the stream.

   NEEDBITS(n) uses PULLBYTE() to get an available byte of input, or to return
   if there is no input available.  The decoding of variable length codes uses
   PULLBYTE() directly in order to pull just enough bytes to decode the next
   code, and no more.

   Some states loop until they get enough input, making sure that enough
   state information is maintained to continue the loop where it left off
   if NEEDBITS() returns in the loop.  For example, want, need, and keep
   would all have to actually be part of the saved state in case NEEDBITS()
   returns:

    case STATEw:
        while (want < need) {
            NEEDBITS(n);
            keep[want++] = BITS(n);
            DROPBITS(n);
        }
        state = STATEx;
    case STATEx:

   As shown above, if the next state is also the next case, then the break
   is omitted.

   A state may also return if there is not enough output space available to
   complete that state.  Those states are copying stored data, writing a
   literal byte, and copying a matching string.

   When returning, a "goto inf_leave" is used to update the total counters,
   update the check value, and determine whether any progress has been made
   during that inflate() call in order to return the proper return code.
   Progress is defined as a change in either strm.avail_in or strm.avail_out.
   When there is a window, goto inf_leave will update the window with the last
   output written.  If a goto inf_leave occurs in the middle of decompression
   and there is no window currently, goto inf_leave will create one and copy
   output to the window for the next call of inflate().

   In this implementation, the flush parameter of inflate() only affects the
   return code (per zlib.h).  inflate() always writes as much as possible to
   strm.next_out, given the space available and the provided input--the effect
   documented in zlib.h of Z_SYNC_FLUSH.  Furthermore, inflate() always defers
   the allocation of and copying into a sliding window until necessary, which
   provides the effect documented in zlib.h for Z_FINISH when the entire input
   stream available.  So the only thing the flush parameter actually does is:
   when flush is set to Z_FINISH, inflate() cannot return Z_OK.  Instead it
   will return Z_BUF_ERROR if it has not reached the end of the stream.
 */

    z_stream strm;
    int flush;
    inflate_state state;
    byte[] next_buffer;
    byte[] put_buffer;
    long next_index;    /* next input */
    long put_index;     /* next output */
    uint have, left;        /* available input and output */
    ulong hold;         /* bit buffer */
    uint bits;              /* bits in bit buffer */
    uint @in, @out;           /* save starting available input and output */
    uint copy;              /* number of stored or match bytes to copy */
    byte[] from_buffer;
    long from_index;    /* where to copy match bytes from */
    code here;                  /* current decoding table entry */
    code last;                  /* parent table entry */
    uint len;               /* length to copy for repeats, bits to drop */
    int ret;                    /* return code */
//#ifdef GUNZIP
    byte[] hbuf = new byte[4];      /* buffer for gzip header crc calculation */
//#endif
    static ushort[] order = new ushort[19] /* permutation of code lengths */
        {16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15};
public int run(z_stream _strm, int _flush)
{
    this.strm = _strm;
    this.flush = _flush;
    if (inflateStateCheck(strm)!=0 || strm.output_buffer == null ||
        (strm.input_buffer == null && strm.avail_in != 0))
        return zlib.Z_STREAM_ERROR;

    state = strm.state;
    if (state.mode == inflate_mode.TYPE) state.mode = inflate_mode.TYPEDO;      /* skip check */
    LOAD();
    @in = have;
    @out = left;
    ret = zlib.Z_OK;
    for (;;)
        switch (state.mode) {
        case inflate_mode.HEAD:
            if (state.wrap == 0) {
                state.mode = inflate_mode.TYPEDO;
                break;
            }
            if (NEEDBITS_(16))
                goto inf_leave;
//#ifdef GUNZIP
            if ((state.wrap & 2) != 0 && hold == 0x8b1f) {  /* gzip header */
                if (state.wbits == 0)
                    state.wbits = 15;
                state.check = crc32.crc32_(0L, null, 0, 0);
                CRC2(state.check, hold);
                INITBITS();
                state.mode = inflate_mode.FLAGS;
                break;
            }
            state.flags = 0;           /* expect zlib header */
            if (state.head != null)
                state.head.done = -1;
            if (!((state.wrap & 1) != 0) ||   /* check if zlib header allowed */
//#else
//            if (
//#endif
                (((BITS(8) << 8) + (hold >> 8)) % 31) != 0) {
                strm.msg = "incorrect header check";
                state.mode = inflate_mode.BAD;
                break;
            }
            if (BITS(4) != zlib.Z_DEFLATED) {
                strm.msg = "unknown compression method";
                state.mode = inflate_mode.BAD;
                break;
            }
            DROPBITS(4);
            len = BITS(4) + 8;
            if (state.wbits == 0)
                state.wbits = len;
            if (len > 15 || len > state.wbits) {
                strm.msg = "invalid window size";
                state.mode = inflate_mode.BAD;
                break;
            }
            state.dmax = 1U << (int)len;
            //Tracev((stderr, "inflate:   zlib header ok\n"));
            strm.adler = state.check = adler32.adler32_(0L, null, 0, 0);
            state.mode = (hold & 0x200) != 0 ? inflate_mode.DICTID : inflate_mode.TYPE;
            INITBITS();
            break;
//#ifdef GUNZIP
        case inflate_mode.FLAGS:
            if (NEEDBITS_(16))
                goto inf_leave;
            state.flags = (int)(hold);
            if ((state.flags & 0xff) != zlib.Z_DEFLATED) {
                strm.msg = "unknown compression method";
                state.mode = inflate_mode.BAD;
                break;
            }
            if ((state.flags & 0xe000) != 0) {
                strm.msg = "unknown header flags set";
                state.mode = inflate_mode.BAD;
                break;
            }
            if (state.head != null)
                state.head.text = (int)((hold >> 8) & 1);
            if ((state.flags & 0x0200) != 0 && (state.wrap & 4) != 0)
                CRC2(state.check, hold);
            INITBITS();
            state.mode = inflate_mode.TIME;
            goto case inflate_mode.TIME;
        case inflate_mode.TIME:
            if (NEEDBITS_(32))
                goto inf_leave;
            if (state.head != null)
                state.head.time = hold;
            if ((state.flags & 0x0200) != 0 && (state.wrap & 4) != 0)
                CRC4(state.check, hold);
            INITBITS();
            state.mode = inflate_mode.OS;
            goto case inflate_mode.OS;
        case inflate_mode.OS:
            if (NEEDBITS_(16))
                goto inf_leave;
            if (state.head != null) {
                state.head.xflags = (int)(hold & 0xff);
                state.head.os = (int)(hold >> 8);
            }
            if ((state.flags & 0x0200) != 0 && (state.wrap & 4) != 0)
                CRC2(state.check, hold);
            INITBITS();
            state.mode = inflate_mode.EXLEN;
            goto case inflate_mode.EXLEN;
        case inflate_mode.EXLEN:
            if ((state.flags & 0x0400) != 0) {
                if (NEEDBITS_(16))
                    goto inf_leave;
                state.length = (uint)(hold);
                if (state.head != null)
                    state.head.extra_len = (uint)hold;
                if ((state.flags & 0x0200) != 0 && (state.wrap & 4) != 0)
                    CRC2(state.check, hold);
                INITBITS();
            }
            else if (state.head != null)
                state.head.extra = null;
            state.mode = inflate_mode.EXTRA;
            goto case inflate_mode.EXTRA;
        case inflate_mode.EXTRA:
            if ((state.flags & 0x0400) != 0) {
                copy = state.length;
                if (copy > have) copy = have;
                if (copy != 0) {
                    if (state.head != null &&
                        state.head.extra != null) {
                        len = state.head.extra_len - state.length;
                        zutil.zmemcpy(state.head.extra, len, next_buffer, next_index,
                                len + copy > state.head.extra_max ?
                                state.head.extra_max - len : copy);
                    }
                    if ((state.flags & 0x0200) != 0 && (state.wrap & 4) != 0)
                        state.check = crc32.crc32_(state.check, next_buffer, next_index, copy);
                    have -= copy;
                    next_index += copy;
                    state.length -= copy;
                }
                if (state.length != 0) goto inf_leave;
            }
            state.length = 0;
            state.mode = inflate_mode.NAME;
            goto case inflate_mode.NAME;
        case inflate_mode.NAME:
            if ((state.flags & 0x0800) != 0) {
                if (have == 0) goto inf_leave;
                copy = 0;
                do {
                    len = (uint)(next_buffer[next_index + copy++]);
                    if (state.head != null &&
                            state.head.name != null &&
                            state.length < state.head.name_max)
                        state.head.name[state.length++] = (byte)len;
                } while (len != 0 && copy < have);
                if ((state.flags & 0x0200) != 0 && (state.wrap & 4) != 0)
                    state.check = crc32.crc32_(state.check, next_buffer, next_index, copy);
                have -= copy;
                next_index += copy;
                if (len != 0) goto inf_leave;
            }
            else if (state.head != null)
                state.head.name = null;
            state.length = 0;
            state.mode = inflate_mode.COMMENT;
            goto case inflate_mode.COMMENT;
        case inflate_mode.COMMENT:
            if ((state.flags & 0x1000) != 0) {
                if (have == 0) goto inf_leave;
                copy = 0;
                do {
                    len = (uint)(next_buffer[next_index + copy++]);
                    if (state.head != null &&
                            state.head.comment != null &&
                            state.length < state.head.comm_max)
                        state.head.comment[state.length++] = (byte)len;
                } while (len != 0 && copy < have);
                if ((state.flags & 0x0200) != 0 && (state.wrap & 4) != 0)
                    state.check = crc32.crc32_(state.check, next_buffer, next_index, copy);
                have -= copy;
                next_index += copy;
                if (len != 0) goto inf_leave;
            }
            else if (state.head != null)
                state.head.comment = null;
            state.mode = inflate_mode.HCRC;
            goto case inflate_mode.HCRC;
        case inflate_mode.HCRC:
            if ((state.flags & 0x0200) != 0) {
                if (NEEDBITS_(16))
                    goto inf_leave;
                if ((state.wrap & 4) != 0 && hold != (state.check & 0xffff)) {
                    strm.msg = "header crc mismatch";
                    state.mode = inflate_mode.BAD;
                    break;
                }
                INITBITS();
            }
            if (state.head != null) {
                state.head.hcrc = (int)((state.flags >> 9) & 1);
                state.head.done = 1;
            }
            strm.adler = state.check = crc32.crc32_(0L, null, 0, 0);
            state.mode = inflate_mode.TYPE;
            break;
//#endif
        case inflate_mode.DICTID:
            if (NEEDBITS_(32))
                goto inf_leave;
            strm.adler = state.check = zutil.ZSWAP32((uint)hold);
            INITBITS();
            state.mode = inflate_mode.DICT;
            goto case inflate_mode.DICT;
        case inflate_mode.DICT:
            if (state.havedict == 0) {
                RESTORE();
                return zlib.Z_NEED_DICT;
            }
            strm.adler = state.check = adler32.adler32_(0L, null, 0, 0);
            state.mode = inflate_mode.TYPE;
            goto case inflate_mode.TYPE;
        case inflate_mode.TYPE:
            if (flush == zlib.Z_BLOCK || flush == zlib.Z_TREES) goto inf_leave;
            goto case inflate_mode.TYPEDO;
        case inflate_mode.TYPEDO:
            if (state.last != 0) {
                BYTEBITS();
                state.mode = inflate_mode.CHECK;
                break;
            }
            if (NEEDBITS_(3))
                goto inf_leave;
            state.last = (int)BITS(1);
            DROPBITS(1);
            switch (BITS(2)) {
            case 0:                             /* stored block */
                //Tracev((stderr, "inflate:     stored block%s\n",
                //        state.last ? " (last)" : ""));
                state.mode = inflate_mode.STORED;
                break;
            case 1:                             /* fixed block */
                fixedtables(state);
                //Tracev((stderr, "inflate:     fixed codes block%s\n",
                //        state.last ? " (last)" : ""));
                state.mode = inflate_mode.LEN_;             /* decode codes */
                if (flush == zlib.Z_TREES) {
                    DROPBITS(2);
                    goto inf_leave;
                }
                break;
            case 2:                             /* dynamic block */
                //Tracev((stderr, "inflate:     dynamic codes block%s\n",
                //        state.last ? " (last)" : ""));
                state.mode = inflate_mode.TABLE;
                break;
            case 3:
                strm.msg = "invalid block type";
                state.mode = inflate_mode.BAD;
                break;
            }
            DROPBITS(2);
            break;
        case inflate_mode.STORED:
            BYTEBITS();                         /* go to byte boundary */
            if (NEEDBITS_(32))
                goto inf_leave;
            if ((hold & 0xffff) != ((hold >> 16) ^ 0xffff)) {
                strm.msg = "invalid stored block lengths";
                state.mode = inflate_mode.BAD;
                break;
            }
            state.length = (uint)hold & 0xffff;
            //Tracev((stderr, "inflate:       stored length %u\n",
            //        state.length));
            INITBITS();
            state.mode = inflate_mode.COPY_;
            if (flush == zlib.Z_TREES) goto inf_leave;
            goto case inflate_mode.COPY_;
        case inflate_mode.COPY_:
            state.mode = inflate_mode.COPY;
            goto case inflate_mode.COPY;
        case inflate_mode.COPY:
            copy = state.length;
            if (copy != 0) {
                if (copy > have) copy = have;
                if (copy > left) copy = left;
                if (copy == 0) goto inf_leave;
                zutil.zmemcpy(put_buffer, put_index, next_buffer, next_index, copy);
                have -= copy;
                next_index += copy;
                left -= copy;
                put_index += copy;
                state.length -= copy;
                break;
            }
            //Tracev((stderr, "inflate:       stored end\n"));
            state.mode = inflate_mode.TYPE;
            break;
        case inflate_mode.TABLE:
            if (NEEDBITS_(14))
                goto inf_leave;
            state.nlen = BITS(5) + 257;
            DROPBITS(5);
            state.ndist = BITS(5) + 1;
            DROPBITS(5);
            state.ncode = BITS(4) + 4;
            DROPBITS(4);
//#ifndef PKZIP_BUG_WORKAROUND
            if (state.nlen > 286 || state.ndist > 30) {
                strm.msg = "too many length or distance symbols";
                state.mode = inflate_mode.BAD;
                break;
            }
//#endif
            //Tracev((stderr, "inflate:       table sizes ok\n"));
            state.have = 0;
            state.mode = inflate_mode.LENLENS;
            goto case inflate_mode.LENLENS;
        case inflate_mode.LENLENS:
            while (state.have < state.ncode) {
                if (NEEDBITS_(3))
                    goto inf_leave;
                state.lens[order[state.have++]] = (ushort)BITS(3);
                DROPBITS(3);
            }
            while (state.have < 19)
                state.lens[order[state.have++]] = 0;
            state.next = 0;
            state.lencode_array = state.codes;
            state.lencode_index = state.next;
            state.lenbits = 7;
            ret = inftrees.inflate_table(codetype.CODES, state.lens, 0, 19, state.codes, ref state.next,
                                ref state.lenbits, state.work);
            if (ret != 0) {
                strm.msg = "invalid code lengths set";
                state.mode = inflate_mode.BAD;
                break;
            }
            //Tracev((stderr, "inflate:       code lengths ok\n"));
            state.have = 0;
            state.mode = inflate_mode.CODELENS;
            goto case inflate_mode.CODELENS;
        case inflate_mode.CODELENS:
            while (state.have < state.nlen + state.ndist) {
                for (;;) {
                    here = state.lencode_array[state.lencode_index + BITS(state.lenbits)];
                    if ((uint)(here.bits) <= bits) break;
                    if (PULLBYTE_())
                        goto inf_leave;
                }
                if (here.val < 16) {
                    DROPBITS(here.bits);
                    state.lens[state.have++] = here.val;
                }
                else {
                    if (here.val == 16) {
                        if (NEEDBITS_(here.bits + 2))
                            goto inf_leave;
                        DROPBITS(here.bits);
                        if (state.have == 0) {
                            strm.msg = "invalid bit length repeat";
                            state.mode = inflate_mode.BAD;
                            break;
                        }
                        len = state.lens[state.have - 1];
                        copy = 3 + BITS(2);
                        DROPBITS(2);
                    }
                    else if (here.val == 17) {
                        if (NEEDBITS_(here.bits + 3))
                            goto inf_leave;
                        DROPBITS(here.bits);
                        len = 0;
                        copy = 3 + BITS(3);
                        DROPBITS(3);
                    }
                    else {
                        if (NEEDBITS_(here.bits + 7))
                            goto inf_leave;
                        DROPBITS(here.bits);
                        len = 0;
                        copy = 11 + BITS(7);
                        DROPBITS(7);
                    }
                    if (state.have + copy > state.nlen + state.ndist) {
                        strm.msg = "invalid bit length repeat";
                        state.mode = inflate_mode.BAD;
                        break;
                    }
                    while (copy-- != 0)
                        state.lens[state.have++] = (ushort)len;
                }
            }

            /* handle error breaks in while */
            if (state.mode == inflate_mode.BAD) break;

            /* check for end-of-block code (better have one) */
            if (state.lens[256] == 0) {
                strm.msg = "invalid code -- missing end-of-block";
                state.mode = inflate_mode.BAD;
                break;
            }

            /* build code tables -- note: do not change the lenbits or distbits
               values here (9 and 6) without reading the comments in inftrees.h
               concerning the ENOUGH constants, which depend on those values */
            state.next = 0;
            state.lencode_array = state.codes;
            state.lencode_index = state.next;
            state.lenbits = 9;
            ret = inftrees.inflate_table(codetype.LENS, state.lens, 0, state.nlen, state.codes, ref state.next,
                                ref state.lenbits, state.work);
            if (ret != 0) {
                strm.msg = "invalid literal/lengths set";
                state.mode = inflate_mode.BAD;
                break;
            }
            state.distcode_array = state.codes;
            state.distcode_index = state.next;
            state.distbits = 6;
            ret = inftrees.inflate_table(codetype.DISTS, state.lens, state.nlen, state.ndist,
                            state.codes, ref state.next, ref state.distbits, state.work);
            if (ret != 0) {
                strm.msg = "invalid distances set";
                state.mode = inflate_mode.BAD;
                break;
            }
            //Tracev((stderr, "inflate:       codes ok\n"));
            state.mode = inflate_mode.LEN_;
            if (flush == zlib.Z_TREES) goto inf_leave;
            goto case inflate_mode.LEN_;
        case inflate_mode.LEN_:
            state.mode = inflate_mode.LEN;
            goto case inflate_mode.LEN;
        case inflate_mode.LEN:
            if (have >= 6 && left >= 258) {
                RESTORE();
                inffast.inflate_fast(strm, @out);
                LOAD();
                if (state.mode == inflate_mode.TYPE)
                    state.back = -1;
                break;
            }
            state.back = 0;
            for (;;) {
                here = state.lencode_array[state.lencode_index + BITS(state.lenbits)];
                if ((uint)(here.bits) <= bits) break;
                if (PULLBYTE_())
                    goto inf_leave;
            }
            if (here.op != 0 && (here.op & 0xf0) == 0) {
                last = here;
                for (;;) {
                    here = state.lencode_array[state.lencode_index + last.val +
                            (BITS(last.bits + last.op) >> last.bits)];
                    if ((uint)(last.bits + here.bits) <= bits) break;
                    if (PULLBYTE_())
                        goto inf_leave;
                }
                DROPBITS(last.bits);
                state.back += last.bits;
            }
            DROPBITS(here.bits);
            state.back += here.bits;
            state.length = (uint)here.val;
            if ((int)(here.op) == 0) {
                //Tracevv((stderr, here.val >= 0x20 && here.val < 0x7f ?
                //        "inflate:         literal '%c'\n" :
                //        "inflate:         literal 0x%02x\n", here.val));
                state.mode = inflate_mode.LIT;
                break;
            }
            if ((here.op & 32) != 0) {
                //Tracevv((stderr, "inflate:         end of block\n"));
                state.back = -1;
                state.mode = inflate_mode.TYPE;
                break;
            }
            if ((here.op & 64) != 0) {
                strm.msg = "invalid literal/length code";
                state.mode = inflate_mode.BAD;
                break;
            }
            state.extra = (uint)(here.op) & 15;
            state.mode = inflate_mode.LENEXT;
            goto case inflate_mode.LENEXT;
        case inflate_mode.LENEXT:
            if (state.extra != 0) {
                if (NEEDBITS_(state.extra))
                    goto inf_leave;
                state.length += BITS(state.extra);
                DROPBITS(state.extra);
                state.back += (int)state.extra;
            }
            //Tracevv((stderr, "inflate:         length %u\n", state.length));
            state.was = state.length;
            state.mode = inflate_mode.DIST;
            goto case inflate_mode.DIST;
        case inflate_mode.DIST:
            for (;;) {
                here = state.distcode_array[state.distcode_index + BITS(state.distbits)];
                if ((uint)(here.bits) <= bits) break;
                if (PULLBYTE_())
                    goto inf_leave;
            }
            if ((here.op & 0xf0) == 0) {
                last = here;
                for (;;) {
                    here = state.distcode_array[state.distcode_index + last.val +
                            (BITS(last.bits + last.op) >> last.bits)];
                    if ((uint)(last.bits + here.bits) <= bits) break;
                    if (PULLBYTE_())
                        goto inf_leave;
                }
                DROPBITS(last.bits);
                state.back += last.bits;
            }
            DROPBITS(here.bits);
            state.back += here.bits;
            if ((here.op & 64) != 0) {
                strm.msg = "invalid distance code";
                state.mode = inflate_mode.BAD;
                break;
            }
            state.offset = (uint)here.val;
            state.extra = (uint)(here.op) & 15;
            state.mode = inflate_mode.DISTEXT;
            goto case inflate_mode.DISTEXT;
        case inflate_mode.DISTEXT:
            if (state.extra != 0) {
                if (NEEDBITS_(state.extra))
                    goto inf_leave;
                state.offset += BITS(state.extra);
                DROPBITS(state.extra);
                state.back += (int)state.extra;
            }
//#ifdef INFLATE_STRICT
//            if (state.offset > state.dmax) {
//                strm.msg = "invalid distance too far back";
//                state.mode = inflate_mode.BAD;
//                break;
//            }
//#endif
            //Tracevv((stderr, "inflate:         distance %u\n", state.offset));
            state.mode = inflate_mode.MATCH;
            goto case inflate_mode.MATCH;
        case inflate_mode.MATCH:
            if (left == 0) goto inf_leave;
            copy = @out - left;
            if (state.offset > copy) {         /* copy from window */
                copy = state.offset - copy;
                if (copy > state.whave) {
                    if (state.sane != 0) {
                        strm.msg = "invalid distance too far back";
                        state.mode = inflate_mode.BAD;
                        break;
                    }
//#ifdef INFLATE_ALLOW_INVALID_DISTANCE_TOOFAR_ARRR
//                    Trace((stderr, "inflate.c too far\n"));
//                    copy -= state.whave;
//                    if (copy > state.length) copy = state.length;
//                    if (copy > left) copy = left;
//                    left -= copy;
//                    state.length -= copy;
//                    do {
//                        *put++ = 0;
//                    } while (--copy);
//                    if (state.length == 0) state.mode = inflate_mode.LEN;
//                    break;
//#endif
                }
                if (copy > state.wnext) {
                    copy -= state.wnext;
                    from_buffer = state.window; from_index = (state.wsize - copy);
                }
                else {
                    from_buffer = state.window; from_index = (state.wnext - copy);
                }
                if (copy > state.length) copy = state.length;
            }
            else {                              /* copy from output */
                from_buffer = put_buffer; from_index = put_index - state.offset;
                copy = state.length;
            }
            if (copy > left) copy = left;
            left -= copy;
            state.length -= copy;
            do {
                put_buffer[put_index++] = from_buffer[from_index++];
            } while (--copy != 0);
            if (state.length == 0) state.mode = inflate_mode.LEN;
            break;
        case inflate_mode.LIT:
            if (left == 0) goto inf_leave;
            put_buffer[put_index++] = (byte)(state.length);
            left--;
            state.mode = inflate_mode.LEN;
            break;
        case inflate_mode.CHECK:
            if (state.wrap != 0) {
                if (NEEDBITS_(32))
                    goto inf_leave;
                @out -= left;
                strm.total_out += @out;
                state.total += @out;
                if ((state.wrap & 4) != 0 && @out != 0)
                    strm.adler = state.check =
                        UPDATE(state.check, put_buffer, put_index - @out, @out);
                @out = left;
                if ((state.wrap & 4) != 0 && (
//#ifdef GUNZIP
                     state.flags != 0 ? hold :
//#endif
                     zutil.ZSWAP32((uint)hold)) != state.check) {
                    strm.msg = "incorrect data check";
                    state.mode = inflate_mode.BAD;
                    break;
                }
                INITBITS();
                //Tracev((stderr, "inflate:   check matches trailer\n"));
            }
//#ifdef GUNZIP
            state.mode = inflate_mode.LENGTH;
            goto case inflate_mode.LENGTH;
        case inflate_mode.LENGTH:
            if (state.wrap != 0 && state.flags != 0) {
                if (NEEDBITS_(32))
                    goto inf_leave;
                if (hold != (state.total & 0xffffffffUL)) {
                    strm.msg = "incorrect length check";
                    state.mode = inflate_mode.BAD;
                    break;
                }
                INITBITS();
                //Tracev((stderr, "inflate:   length matches trailer\n"));
            }
//#endif
            state.mode = inflate_mode.DONE;
            goto case inflate_mode.DONE;
        case inflate_mode.DONE:
            ret = zlib.Z_STREAM_END;
            goto inf_leave;
        case inflate_mode.BAD:
            ret = zlib.Z_DATA_ERROR;
            goto inf_leave;
        case inflate_mode.MEM:
            return zlib.Z_MEM_ERROR;
        case inflate_mode.SYNC:
        default:
            return zlib.Z_STREAM_ERROR;
        }

    /*
       Return from inflate(), updating the total counts and the check value.
       If there was no progress during the inflate() call, return a buffer
       error.  Call updatewindow() to create and/or update the window state.
       Note: a memory error from inflate() is non-recoverable.
     */
  inf_leave:
    RESTORE();
    if (state.wsize != 0 || (@out != strm.avail_out && state.mode < inflate_mode.BAD &&
            (state.mode < inflate_mode.CHECK || flush != zlib.Z_FINISH)))
        if (updatewindow(strm, strm.output_buffer, strm.next_out, @out - strm.avail_out) != 0) {
            state.mode = inflate_mode.MEM;
            return zlib.Z_MEM_ERROR;
        }
    @in -= strm.avail_in;
    @out -= strm.avail_out;
    strm.total_in += @in;
    strm.total_out += @out;
    state.total += @out;
    if ((state.wrap & 4) != 0 && @out != 0)
        strm.adler = state.check =
            UPDATE(state.check, strm.output_buffer, strm.next_out - @out, @out);
    strm.data_type = (int)state.bits + (state.last != 0 ? 64 : 0) +
                      (state.mode == inflate_mode.TYPE ? 128 : 0) +
                      (state.mode == inflate_mode.LEN_ || state.mode == inflate_mode.COPY_ ? 256 : 0);
    if (((@in == 0 && @out == 0) || flush == zlib.Z_FINISH) && ret == zlib.Z_OK)
        ret = zlib.Z_BUF_ERROR;
    return ret;
}


public static int inflate_(z_stream strm, int flush) {
	return new inflate().run(strm, flush);
}
	}
}
