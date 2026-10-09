import { CalendarDays, Plus, Wallet } from "lucide-react";
import { Link } from "react-router-dom";
import { DataState, PageTitle } from "../components/UI";
import { useResource } from "../lib/hooks";
import type { Balance } from "../lib/types";

export function Balances() {
  const balances = useResource<Balance[]>("/LeaveRequests/my-balance");
  return (
    <>
      <PageTitle
        eyebrow="MAKE SPACE TO RECHARGE"
        title="My leave balances"
        description="See what you’ve used, what’s reserved, and what’s still yours to plan."
        action={
          <Link to="/requests?new=1" className="btn btn-primary">
            <Plus size={17} />
            Request time off
          </Link>
        }
      />
      <DataState
        loading={balances.loading}
        error={balances.error}
        retry={balances.reload}
        empty={!balances.data?.length}
        emptyTitle="No leave balances allocated"
        emptyDescription="Contact your administrator to set up your leave allocation."
      >
        <div className="balance-cards">
          {balances.data?.map((balance) => (
            <section className="panel balance-card" key={balance.id}>
              <div className="balance-card-title">
                <div className="stat-icon sage">
                  <Wallet size={21} />
                </div>
                <h2>{balance.leaveTypeName}</h2>
              </div>
              <div className="balance-available">
                <strong>{balance.remainingDays}</strong>
                <span>days available</span>
              </div>
              <div className="balance-track">
                <span
                  style={{
                    width: `${balance.allocatedDays ? Math.min(100, (balance.usedDays / balance.allocatedDays) * 100) : 0}%`,
                  }}
                />
                <span
                  className="reserved"
                  style={{
                    width: `${balance.allocatedDays ? Math.min(100, (balance.pendingDays / balance.allocatedDays) * 100) : 0}%`,
                  }}
                />
              </div>
              <div className="balance-breakdown">
                <div>
                  <span>
                    <i className="dot used" />
                    Used
                  </span>
                  <strong>{balance.usedDays}</strong>
                </div>
                <div>
                  <span>
                    <i className="dot pending" />
                    Reserved
                  </span>
                  <strong>{balance.pendingDays}</strong>
                </div>
                <div>
                  <span>Allocated</span>
                  <strong>{balance.allocatedDays}</strong>
                </div>
              </div>
            </section>
          ))}
        </div>
      </DataState>
      <div className="info-note">
        <CalendarDays size={20} />
        <div>
          <strong>A quick note about your balance</strong>
          <p>
            Pending requests reserve days until your manager makes a decision.
            Rejected or cancelled requests return those days to your available
            balance. Friday and Saturday are not counted.
          </p>
        </div>
      </div>
    </>
  );
}
