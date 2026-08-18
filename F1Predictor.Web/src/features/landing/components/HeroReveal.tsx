"use client";

import { motion, useReducedMotion, type Variants } from "motion/react";
import type { ReactNode } from "react";

const container: Variants = {
  hidden: {},
  show: { transition: { staggerChildren: 0.12 } },
};

const item: Variants = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0, transition: { duration: 0.6, ease: [0.16, 1, 0.3, 1] } },
};

interface HeroRevealProps {
  children: ReactNode;
  className?: string;
}

/** Staggers its HeroRevealItem children in on mount — the hero is above the fold, so this
 * triggers immediately rather than on scroll (compare RevealOnScroll for the rest of the page). */
export function HeroReveal({ children, className }: HeroRevealProps) {
  const reduce = useReducedMotion();

  return (
    <motion.div className={className} variants={container} initial={reduce ? "show" : "hidden"} animate="show">
      {children}
    </motion.div>
  );
}

export function HeroRevealItem({ children, className }: HeroRevealProps) {
  return (
    <motion.div className={className} variants={item}>
      {children}
    </motion.div>
  );
}
