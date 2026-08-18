"use client";

import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

interface RevealOnScrollProps {
  children: ReactNode;
  /** Stagger delay in seconds, e.g. index * 0.08 for a list. */
  delay?: number;
  className?: string;
}

/**
 * Fades a section in as it enters the viewport, once. Motivated as "reveal content in
 * sequence" per the landing page's design-taste-frontend skill, not decoration for its
 * own sake — used for lists of cards/steps, not single hero copy (see HeroReveal for that).
 */
export function RevealOnScroll({ children, delay = 0, className }: RevealOnScrollProps) {
  const reduce = useReducedMotion();

  return (
    <motion.div
      className={className}
      initial={reduce ? false : { opacity: 0, y: 24 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{ duration: 0.6, delay, ease: [0.16, 1, 0.3, 1] }}
    >
      {children}
    </motion.div>
  );
}

/**
 * Same reveal as RevealOnScroll, rendered as an `<li>` — for use directly inside `<ol>`/`<ul>`,
 * where a wrapping `<div>` would be invalid markup.
 */
export function RevealListItemOnScroll({ children, delay = 0, className }: RevealOnScrollProps) {
  const reduce = useReducedMotion();

  return (
    <motion.li
      className={className}
      initial={reduce ? false : { opacity: 0, y: 24 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{ duration: 0.6, delay, ease: [0.16, 1, 0.3, 1] }}
    >
      {children}
    </motion.li>
  );
}
