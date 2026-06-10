import { test, expect } from "vitest";
import { prossimo, mescola } from "../src/rng.js";

test("stesso seed -> stessa sequenza", () => {
  const a = prossimo(123);
  const b = prossimo(123);
  expect(a.val).toBe(b.val);
  expect(a.stato).toBe(b.stato);
});

test("avanza lo stato (val successivi diversi)", () => {
  const a = prossimo(123);
  const b = prossimo(a.stato);
  expect(a.val).not.toBe(b.val);
  expect(a.val).toBeGreaterThanOrEqual(0);
  expect(a.val).toBeLessThan(1);
});

test("mescola: deterministico e permutazione valida", () => {
  const arr = [1, 2, 3, 4, 5];
  const r1 = mescola(arr, 42);
  const r2 = mescola(arr, 42);
  expect(r1.arr).toEqual(r2.arr);
  expect(r1.arr.slice().sort()).toEqual([1, 2, 3, 4, 5]);
  expect(arr).toEqual([1, 2, 3, 4, 5]); // input non mutato
});
