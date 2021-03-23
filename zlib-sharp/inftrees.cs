// port of inftrees.h and inftrees.c

namespace zlib_sharp {
	/* inftrees.h -- header to use inftrees.c
	 * Copyright (C) 1995-2005, 2010 Mark Adler
	 * For conditions of distribution and use, see copyright notice in zlib.h
	 */
	/* inftrees.c -- generate Huffman trees for efficient decoding
     * Copyright (C) 1995-2017 Mark Adler
     * For conditions of distribution and use, see copyright notice in zlib.h
     */

	/* WARNING: this file should *not* be used by applications. It is
	   part of the implementation of the compression library and is
	   subject to change. Applications should only use zlib.h.
	 */

	/* Structure for decoding tables.  Each entry provides either the
	   information needed to do the operation requested by the code that
	   indexed that table entry, or it provides a pointer to another
	   table that indexes more bits of the code.  op indicates whether
	   the entry is a pointer to another table, a literal, a length or
	   distance, an end-of-block, or an invalid code.  For a table
	   pointer, the low four bits of op is the number of index bits of
	   that table.  For a length or distance, the low four bits of op
	   is the number of extra bits to get after the code.  bits is
	   the number of bits in this code or part of the code to drop off
	   of the bit buffer.  val is the actual byte to output in the case
	   of a literal, the base length or distance, or the offset from
	   the current table to the next table.  Each entry is four bytes. */
	internal struct code {
		public byte op;           /* operation, extra bits, table bits */
		public byte bits;         /* bits in this part of the code */
		public ushort val;        /* offset in table or code value */

		public code(byte op, byte bits, ushort val) {
			this.op = op;
			this.bits = bits;
			this.val = val;
		}
	}

	/* op values as set by inflate_table():
		00000000 - literal
		0000tttt - table link, tttt != 0 is the number of table index bits
		0001eeee - length or distance, eeee is the number of extra bits
		01100000 - end of block
		01000000 - invalid code
	 */

	/* Type of code to build for inflate_table() */
	enum codetype {
		CODES,
		LENS,
		DISTS
	}

	/* Maximum size of the dynamic table.  The maximum number of code structures is
	   1444, which is the sum of 852 for literal/length codes and 592 for distance
	   codes.  These values were found by exhaustive searches using the program
	   examples/enough.c found in the zlib distribtution.  The arguments to that
	   program are the number of symbols, the initial root table size, and the
	   maximum bit length of a code.  "enough 286 9 15" for literal/length codes
	   returns returns 852, and "enough 30 6 15" for distance codes returns 592.
	   The initial root table size (9 or 6) is found in the fifth argument of the
	   inflate_table() calls in inflate.c and infback.c.  If the root table size is
	   changed, then these maximum sizes would be need to be recalculated and
	   updated. */
	class inftrees {
		public const int ENOUGH_LENS = 852;
		public const int ENOUGH_DISTS = 592;
		public const int ENOUGH = ENOUGH_LENS + ENOUGH_DISTS;

		public const int MAXBITS = 15;

		/*
          If you use the zlib library in a product, an acknowledgment is welcome
          in the documentation of your product. If for some reason you cannot
          include such an acknowledgment, I would appreciate that you keep this
          copyright string in the executable of your product.
         */
		public const string inflate_copyright = " inflate 1.2.11 Copyright 1995-2017 Mark Adler ";



		/*
		   Build a set of tables to decode the provided canonical Huffman code.
		   The code lengths are lens[0..codes-1].  The result starts at *table,
		   whose indices are 0..2^bits-1.  work is a writable array of at least
		   lens shorts, which is used as a work area.  type is the type of code
		   to be generated, CODES, LENS, or DISTS.  On return, zero is success,
		   -1 is an invalid code, and +1 means that ENOUGH isn't enough.  table
		   on return points to the next available entry's address.  bits is the
		   requested root table index bits, and on return it is the actual root
		   table index bits.  It will differ if the request is greater than the
		   longest code or if it is less than the shortest code.
		 */
		internal static int inflate_table(
		codetype type,
		ushort[] lens_array,
		long lens_index,
		uint codes,
		code[] table_array,
		ref long table_index,
		ref uint bits,
		ushort[] work
		) {
			uint len;               /* a code's length in bits */
			uint sym;               /* index of code symbols */
			uint min, max;          /* minimum and maximum code lengths */
			uint root;              /* number of index bits for root table */
			uint curr;              /* number of index bits for current table */
			uint drop;              /* code bits to drop for sub-table */
			int left;                   /* number of prefix codes available */
			uint used;              /* code entries in table used */
			uint huff;              /* Huffman code */
			uint incr;              /* for incrementing code, index */
			uint fill;              /* index for replicating entries */
			uint low;               /* low bits for current root entry */
			uint mask;              /* mask for low root bits */
			code here = new code(); /* table entry for duplication */
			long next;             /* next available space in table */
			ushort[] @base;     /* base value table to use */
			ushort[] extra;    /* extra bits table to use */
			uint match;             /* use base and extra for symbol >= match */
			int MAXBITS = inftrees.MAXBITS;
			ushort[] count = new ushort[MAXBITS + 1];    /* number of codes of each length */
			ushort[] offs = new ushort[MAXBITS + 1];     /* offsets in table for each length */
			/*static const*/
			ushort[] lbase = new ushort[31] { /* Length codes 257..285 base */
        3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
		35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0};
			/*static const*/
			ushort[] lext = new ushort[31] { /* Length codes 257..285 extra */
        16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17, 18, 18, 18, 18,
		19, 19, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21, 16, 77, 202};
			/*static const*/
			ushort[] dbase = new ushort[32] { /* Distance codes 0..29 base */
        1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
		257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
		8193, 12289, 16385, 24577, 0, 0};
			/*static const*/
			ushort[] dext = new ushort[32] { /* Distance codes 0..29 extra */
        16, 16, 16, 16, 17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22,
		23, 23, 24, 24, 25, 25, 26, 26, 27, 27,
		28, 28, 29, 29, 64, 64};

			/*
			   Process a set of code lengths to create a canonical Huffman code.  The
			   code lengths are lens[0..codes-1].  Each length corresponds to the
			   symbols 0..codes-1.  The Huffman code is generated by first sorting the
			   symbols by length from short to long, and retaining the symbol order
			   for codes with equal lengths.  Then the code starts with all zero bits
			   for the first code of the shortest length, and the codes are integer
			   increments for the same length, and zeros are appended as the length
			   increases.  For the deflate format, these bits are stored backwards
			   from their more natural integer increment ordering, and so when the
			   decoding tables are built in the large loop below, the integer codes
			   are incremented backwards.

			   This routine assumes, but does not check, that all of the entries in
			   lens[] are in the range 0..MAXBITS.  The caller must assure this.
			   1..MAXBITS is interpreted as that code length.  zero means that that
			   symbol does not occur in this code.

			   The codes are sorted by computing a count of codes for each length,
			   creating from that a table of starting indices for each length in the
			   sorted table, and then entering the symbols in order in the sorted
			   table.  The sorted table is work[], with that space being provided by
			   the caller.

			   The length counts are used for other purposes as well, i.e. finding
			   the minimum and maximum length codes, determining if there are any
			   codes at all, checking for a valid set of lengths, and looking ahead
			   at length counts to determine sub-table sizes when building the
			   decoding tables.
			 */

			/* accumulate lengths for codes (assumes lens[] all in 0..MAXBITS) */
			for (len = 0; len <= MAXBITS; len++)
				count[len] = 0;
			for (sym = 0; sym < codes; sym++)
				count[lens_array[lens_index + sym]]++;

			/* bound code lengths, force root to be within code lengths */
			root = bits;
			for (max = (uint)MAXBITS; max >= 1; max--)
				if (count[max] != 0) break;
			if (root > max) root = max;
			if (max == 0) {                     /* no symbols to code at all */
				here = new code(64, 1, 0);    /* invalid code marker */
				table_array[table_index++] = here;             /* make a table to force an error */
				table_array[table_index++] = here;
				bits = 1;
				return 0;     /* no symbols, but wait for decoding to report error */
			}
			for (min = 1; min < max; min++)
				if (count[min] != 0) break;
			if (root < min) root = min;

			/* check for an over-subscribed or incomplete set of lengths */
			left = 1;
			for (len = 1; len <= MAXBITS; len++) {
				left <<= 1;
				left -= count[len];
				if (left < 0) return -1;        /* over-subscribed */
			}
			codetype CODES = codetype.CODES;
			if (left > 0 && (type == CODES || max != 1))
				return -1;                      /* incomplete set */

			/* generate offsets into symbol table for each length for sorting */
			offs[1] = 0;
			for (len = 1; len < MAXBITS; len++)
				offs[len + 1] = (ushort)(offs[len] + count[len]);

			/* sort symbols by length, by symbol order within each length */
			for (sym = 0; sym < codes; sym++)
				if (lens_array[lens_index + sym] != 0) work[offs[lens_array[lens_index + sym]]++] = (ushort)sym;

			/*
			   Create and fill in decoding tables.  In this loop, the table being
			   filled is at next and has curr index bits.  The code being used is huff
			   with length len.  That code is converted to an index by dropping drop
			   bits off of the bottom.  For codes where len is less than drop + curr,
			   those top drop + curr - len bits are incremented through all values to
			   fill the table with replicated entries.

			   root is the number of index bits for the root table.  When len exceeds
			   root, sub-tables are created pointed to by the root entry with an index
			   of the low root bits of huff.  This is saved in low to check for when a
			   new sub-table should be started.  drop is zero when the root table is
			   being filled, and drop is root when sub-tables are being filled.

			   When a new sub-table is needed, it is necessary to look ahead in the
			   code lengths to determine what size sub-table is needed.  The length
			   counts are used for this, and so count[] is decremented as codes are
			   entered in the tables.

			   used keeps track of how many table entries have been allocated from the
			   provided *table space.  It is checked for LENS and DIST tables against
			   the constants ENOUGH_LENS and ENOUGH_DISTS to guard against changes in
			   the initial root table size constants.  See the comments in inftrees.h
			   for more information.

			   sym increments through all symbols, and the loop terminates when
			   all codes of length max, i.e. all codes, have been processed.  This
			   routine permits incomplete codes, so another loop after this one fills
			   in the rest of the decoding tables with invalid code markers.
			 */

			/* set up for code type */
			switch (type) {
				case codetype.CODES:
					@base = extra = work;    /* dummy value--not used */
					match = 20;
					break;
				case codetype.LENS:
					@base = lbase;
					extra = lext;
					match = 257;
					break;
				default:    /* DISTS */
					@base = dbase;
					extra = dext;
					match = 0;
					break;
			}

			/* initialize state for loop */
			huff = 0;                   /* starting code */
			sym = 0;                    /* starting code symbol */
			len = min;                  /* starting code length */
			next = table_index;              /* current table to fill in */
			curr = root;                /* current table index bits */
			drop = 0;                   /* current bits to drop from code for index */
			low = uint.MaxValue;       /* trigger new sub-table when len > root */
			used = 1U << ((int)root);          /* use root table entries */
			mask = used - 1;            /* mask for comparing low */

			/* check available table space */
			if ((type == codetype.LENS && used > inftrees.ENOUGH_LENS) ||
				(type == codetype.DISTS && used > inftrees.ENOUGH_DISTS))
				return 1;

			/* process all codes and make table entries */
			for (; ; ) {
				/* create table entry */
				byte here_bits = (byte)(len - drop);
				byte here_op;
				ushort here_val;
				if (work[sym] + 1U < match) {
					here_op = (byte)0;
					here_val = work[sym];
				} else if (work[sym] >= match) {
					here_op = (byte)(extra[work[sym] - match]);
					here_val = @base[work[sym] - match];
				} else {
					here_op = (byte)(32 + 64);         /* end of block */
					here_val = 0;
				}
				here = new code(here_op, here_bits, here_val);

				/* replicate for those indices with low len bits equal to huff */
				incr = 1U << (int)(len - drop);
				fill = 1U << (int)(curr);
				min = fill;                 /* save offset to next table */
				do {
					fill -= incr;
					table_array[next + ((huff >> (int)drop) + fill)] = here;
				} while (fill != 0);

				/* backwards increment the len-bit code huff */
				incr = 1U << (int)(len - 1);
				while ((huff & incr) != 0)
					incr >>= 1;
				if (incr != 0) {
					huff &= incr - 1;
					huff += incr;
				} else
					huff = 0;

				/* go to next symbol, update count, len */
				sym++;
				if (--(count[len]) == 0) {
					if (len == max) break;
					len = lens_array[lens_index + work[sym]];
				}

				/* create new sub-table if needed */
				if (len > root && (huff & mask) != low) {
					/* if first time, transition to sub-tables */
					if (drop == 0)
						drop = root;

					/* increment past last table */
					next += min;            /* here min is 1 << curr */

					/* determine length of next table */
					curr = len - drop;
					left = (int)(1 << (int)curr);
					while (curr + drop < max) {
						left -= count[curr + drop];
						if (left <= 0) break;
						curr++;
						left <<= 1;
					}

					/* check for enough space */
					used += 1U << (int)curr;
					if ((type == codetype.LENS && used > ENOUGH_LENS) ||
						(type == codetype.DISTS && used > ENOUGH_DISTS))
						return 1;

					/* point entry in root table to sub-table */
					low = huff & mask;
					table_array[table_index + low] = new code((byte)curr, (byte)root, (ushort)(next - table_index));
				}
			}

			/* fill in remaining table entry if code is incomplete (guaranteed to have
			   at most one remaining entry, since if the code is incomplete, the
			   maximum code length that was allowed to get this far is one bit) */
			if (huff != 0) {
				here = new code(64, (byte)(len - drop), 0); /* invalid code marker */
				table_array[next + huff] = here;
			}

			/* set return parameters */
			table_index += used;
			bits = root;
			return 0;
		}
	}
}
