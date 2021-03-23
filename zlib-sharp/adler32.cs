// port of adler32.c

namespace zlib_sharp {
	internal static class adler32 {
		/* adler32.c -- compute the Adler-32 checksum of a data stream
		 * Copyright (C) 1995-2011, 2016 Mark Adler
		 * For conditions of distribution and use, see copyright notice in zlib.h
		 */

		const uint BASE = 65521U;     /* largest prime smaller than 65536 */
		const uint NMAX = 5552;
		/* NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1 */

		//#define DO1(buf,i)  {adler += (buf)[i]; sum2 += adler;}
		//#define DO2(buf,i)  DO1(buf,i); DO1(buf,i+1);
		//#define DO4(buf,i)  DO2(buf,i); DO2(buf,i+2);
		//#define DO8(buf,i)  DO4(buf,i); DO4(buf,i+4);
		//#define DO16(buf)   DO8(buf,0); DO8(buf,8);

		/* ========================================================================= */
		public static ulong adler32_z(
			ulong adler,
			byte[] buf_array,
			long buf_index,
			ulong len) {
			ulong sum2;
			uint n;

			/* split Adler-32 into component sums */
			sum2 = (adler >> 16) & 0xffff;
			adler &= 0xffff;

			/* in case user likes doing a byte at a time, keep it fast */
			if (len == 1) {
				adler += buf_array[buf_index];
				if (adler >= BASE)
					adler -= BASE;
				sum2 += adler;
				if (sum2 >= BASE)
					sum2 -= BASE;
				return adler | (sum2 << 16);
			}

			/* initial Adler-32 value (deferred check for len == 1 speed) */
			if (buf_array == null)
				return 1L;

			/* in case short lengths are provided, keep it somewhat fast */
			if (len < 16) {
				while (len-- != 0) {
					adler += buf_array[buf_index++];
					sum2 += adler;
				}
				if (adler >= BASE)
					adler -= BASE;
				sum2 %= BASE;            /* only added so many BASE's */
				return adler | (sum2 << 16);
			}

			/* do length NMAX blocks -- requires just one modulo operation */
			while (len >= NMAX) {
				len -= NMAX;
				n = NMAX / 16;          /* NMAX is divisible by 16 */
				do {
					//DO16(buf);          /* 16 sums unrolled */
					adler += buf_array[buf_index + 0]; sum2 += adler;
					adler += buf_array[buf_index + 1]; sum2 += adler;
					adler += buf_array[buf_index + 2]; sum2 += adler;
					adler += buf_array[buf_index + 3]; sum2 += adler;
					adler += buf_array[buf_index + 4]; sum2 += adler;
					adler += buf_array[buf_index + 5]; sum2 += adler;
					adler += buf_array[buf_index + 6]; sum2 += adler;
					adler += buf_array[buf_index + 7]; sum2 += adler;
					adler += buf_array[buf_index + 8]; sum2 += adler;
					adler += buf_array[buf_index + 9]; sum2 += adler;
					adler += buf_array[buf_index + 10]; sum2 += adler;
					adler += buf_array[buf_index + 11]; sum2 += adler;
					adler += buf_array[buf_index + 12]; sum2 += adler;
					adler += buf_array[buf_index + 13]; sum2 += adler;
					adler += buf_array[buf_index + 14]; sum2 += adler;
					adler += buf_array[buf_index + 15]; sum2 += adler;
					buf_index += 16;
				} while (--n != 0);
				adler %= BASE;
				sum2 %= BASE;
			}

			/* do remaining bytes (less than NMAX, still just one modulo) */
			if (len != 0) {                  /* avoid modulos if none remaining */
				while (len >= 16) {
					len -= 16;
					//DO16(buf);
					adler += buf_array[buf_index + 0]; sum2 += adler;
					adler += buf_array[buf_index + 1]; sum2 += adler;
					adler += buf_array[buf_index + 2]; sum2 += adler;
					adler += buf_array[buf_index + 3]; sum2 += adler;
					adler += buf_array[buf_index + 4]; sum2 += adler;
					adler += buf_array[buf_index + 5]; sum2 += adler;
					adler += buf_array[buf_index + 6]; sum2 += adler;
					adler += buf_array[buf_index + 7]; sum2 += adler;
					adler += buf_array[buf_index + 8]; sum2 += adler;
					adler += buf_array[buf_index + 9]; sum2 += adler;
					adler += buf_array[buf_index + 10]; sum2 += adler;
					adler += buf_array[buf_index + 11]; sum2 += adler;
					adler += buf_array[buf_index + 12]; sum2 += adler;
					adler += buf_array[buf_index + 13]; sum2 += adler;
					adler += buf_array[buf_index + 14]; sum2 += adler;
					adler += buf_array[buf_index + 15]; sum2 += adler;
					buf_index += 16;
				}
				while (len-- != 0) {
					adler += buf_array[buf_index++];
					sum2 += adler;
				}
				adler %= BASE;
				sum2 %= BASE;
			}

			/* return recombined sums */
			return adler | (sum2 << 16);
		}

		/* ========================================================================= */
		public static ulong adler32_(
			ulong adler,
			byte[] buf_array,
			long buf_index,
			uint len) {
			return adler32_z(adler, buf_array, buf_index, len);
		}

		/* ========================================================================= */
		private static ulong adler32_combine_(
			ulong adler1,
			ulong adler2,
			long len2) {
			ulong sum1;
			ulong sum2;
			uint rem;

			/* for negative len, return invalid adler32 as a clue for debugging */
			if (len2 < 0)
				return 0xffffffffUL;

			/* the derivation of this formula is left as an exercise for the reader */
			len2 %= BASE;                /* assumes len2 >= 0 */
			rem = (uint)len2;
			sum1 = adler1 & 0xffff;
			sum2 = rem * sum1;
			sum2 %= BASE;
			sum1 += (adler2 & 0xffff) + BASE - 1;
			sum2 += ((adler1 >> 16) & 0xffff) + ((adler2 >> 16) & 0xffff) + BASE - rem;
			if (sum1 >= BASE) sum1 -= BASE;
			if (sum1 >= BASE) sum1 -= BASE;
			if (sum2 >= ((ulong)BASE << 1)) sum2 -= ((ulong)BASE << 1);
			if (sum2 >= BASE) sum2 -= BASE;
			return sum1 | (sum2 << 16);
		}

		/* ========================================================================= */
		public static ulong adler32_combine(
			ulong adler1,
			ulong adler2,
			int len2) {
			return adler32_combine_(adler1, adler2, len2);
		}

		public static ulong adler32_combine64(
			ulong adler1,
			ulong adler2,
			long len2) {
			return adler32_combine_(adler1, adler2, len2);
		}
	}
}
