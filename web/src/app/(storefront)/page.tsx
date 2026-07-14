import Link from "next/link";

export default function StorefrontPage() {
  return (
    <>
      <section className="shell grid gap-8 py-8 md:grid-cols-[1.15fr_.85fr] md:py-16">
        <div className="flex flex-col justify-between py-3">
          <p className="text-xs font-bold tracking-[.2em] text-[#ec6b45]">AMMAN · EVENT EQUIPMENT</p>
          <h1 className="display mt-8 max-w-2xl text-6xl leading-[.86] tracking-[-.045em] sm:text-8xl">Every good room starts with a clean load-in.</h1>
          <div className="mt-10 flex flex-wrap items-center gap-4">
            <a href="#equipment" className="bg-[#0c2633] px-5 py-3 text-sm font-bold text-[#eef2e8]">Browse equipment</a>
            <Link href="/account" className="border border-[#0c2633] px-5 py-3 text-sm font-bold">Manage a rental</Link>
          </div>
        </div>
        <div className="crate grid-noise p-6 text-[#eef2e8]">
          <div className="relative z-10 flex h-full flex-col justify-between">
            <p className="font-mono text-xs tracking-[.16em]">CASE NO. 024 / SOUND + LIGHT</p>
            <div><p className="display text-6xl leading-none">READY<br />TO ROLL</p><p className="mt-3 max-w-xs text-sm text-[#c6d5d2]">Speakers, lighting, stages and the practical kit that makes a room work.</p></div>
            <p className="font-mono text-xs">HANDLE WITH INTENT</p>
          </div>
        </div>
      </section>
      <section id="equipment" className="border-y border-[#0c2633] bg-[#b7d77a] py-8">
        <div className="shell grid gap-6 sm:grid-cols-3"><h2 className="display text-3xl">Sound systems</h2><h2 className="display text-3xl">Lighting rigs</h2><h2 className="display text-3xl">Stages & seating</h2></div>
      </section>
      <section id="how-it-works" className="shell grid gap-8 py-16 md:grid-cols-3">
        <h2 className="display text-4xl leading-none">Plan the room.<br />We stage the rest.</h2>
        <p className="text-sm leading-6">Choose the equipment and your event dates. Availability and booking workflows arrive in later milestones.</p>
        <p className="text-sm leading-6">This responsive shell is ready for customer, operations, and admin surfaces without exposing protected functions.</p>
      </section>
    </>
  );
}
